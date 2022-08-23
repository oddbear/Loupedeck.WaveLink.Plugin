using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin
{
    class WaveLinkClient : IDisposable
    {
        private readonly WaveLinkPlugin _plugin;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Channel<object> _channel;

        private bool _shouldConnect = false;

        private ElgatoWaveClient _client;
        internal bool IsConnected => _client?.IsConnected ?? false;

        internal event EventHandler<List<MonitorMixList>> LocalMonitorOutputFetched;
        internal event EventHandler<MonitoringState> OutputMixerChanged;
        internal event EventHandler<string> LocalMonitorOutputChanged;
        internal event EventHandler<List<ChannelInfo>> ChannelsChanged;
        internal event EventHandler<ChannelInfo> InputMixerChanged;

        public WaveLinkClient(WaveLinkPlugin plugin)
        {
            _plugin = plugin;
            
            _channel = Channel.CreateUnbounded<object>();

            var reconnectThread = new Thread(Reconnect);
            reconnectThread.Start();

            var readChannelThread = new Thread(ReadChannel);
            readChannelThread.Start();
        }

        private async Task UpdateWaveLinkState()
        {
            if (_client is null)
                return;

            var channelWriter = _channel.Writer;

            var monitoringState = await _client.GetMonitoringState();
            channelWriter.TryWrite(monitoringState);

            var mixOutputList = await _client.GetMonitorMixOutputList();
            channelWriter.TryWrite(mixOutputList?.MonitorMixList);
            channelWriter.TryWrite(mixOutputList?.MonitorMix);

            var channels = await _client.GetAllChannelInfo();
            channelWriter.TryWrite(channels);
        }

        private void ReadChannel()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var channelReader = _channel.Reader;
                if (!channelReader.TryRead(out var item))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }

                switch (item)
                {
                    case List<MonitorMixList> monitorMixList:
                        LocalMonitorOutputFetched?.Invoke(this, monitorMixList);
                        continue;
                    case MonitoringState state:
                        OutputMixerChanged?.Invoke(this, state);
                        continue;
                    case string str:
                        LocalMonitorOutputChanged?.Invoke(this, str);
                        continue;
                    case List<ChannelInfo> list:
                        ChannelsChanged?.Invoke(this, list);
                        continue;
                    case ChannelInfo info:
                        InputMixerChanged?.Invoke(this, info);
                        continue;
                }
            }
        }

        private void RefreshClient()
        {
            _client?.Disconnect();
            
            var channelWriter = _channel.Writer;
            _client = new ElgatoWaveClient();
            _client.OutputMixerChanged += (sender, state) => channelWriter.TryWrite(state);
            _client.LocalMonitorOutputChanged += (sender, s) => channelWriter.TryWrite(s);
            _client.ChannelsChanged += (sender, list) => channelWriter.TryWrite(list);
            _client.InputMixerChanged += (sender, info) => channelWriter.TryWrite(info);
        }

        public void Start()
        {
            _shouldConnect = true;
        }

        public void Stop()
        {
            _shouldConnect = false;
            RefreshClient();
        }

        private void Reconnect()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (!_shouldConnect)
                        continue;

                    if (_client is null)
                        RefreshClient();

                    if (_client?.IsConnected != false)
                        continue;

                    var success = _client.ConnectAsync()
                        .GetAwaiter()
                        .GetResult();

                    if (success)
                    {
                        UpdateWaveLinkState()
                            .GetAwaiter()
                            .GetResult();

                        _plugin.ConnectedStatus();
                    }
                    else
                    {
                        _plugin.DisconnectedStatus();
                    }
                }
                catch (ElgatoException exception)
                    when (exception.Message == "Looped through possible ports 2 times and couldn't connect [1824-1834]")
                {
                    _plugin.DisconnectedStatus();

                    RefreshClient();
                }
                catch (Exception exception)
                {
                    _plugin.ErrorStatus(exception.Message);

                    RefreshClient();
                }
                finally
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _client?.Disconnect();
        }

        public MonitorMixOutputList SetMonitorMixOutput(string mixOutput)
        {
            return _client.SetMonitorMixOutput(mixOutput)
                .GetAwaiter().GetResult();
        }

        public ChannelInfo SetInputMixer(ChannelInfo info, MixType mixType)
        {
            return _client.SetInputMixer(info, mixType)
                .GetAwaiter().GetResult();
        }

        public MonitoringState SetOutputMixer(MonitoringState state)
        {
            return _client.SetOutputMixer(state)
                .GetAwaiter().GetResult();
        }
    }
}
