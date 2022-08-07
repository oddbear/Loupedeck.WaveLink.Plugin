using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin
{
    public class WaveLinkPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;
        
        public ElgatoWaveClient Client;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<IEnumerable<MonitorMixList>> LocalMonitorOutputFetched;
        public event EventHandler<IEnumerable<ChannelInfo>> AllChannelInfoFetched;
        
        public WaveLinkPlugin()
        {
            Client = new ElgatoWaveClient();
        }

        public override void Load()
        {
            var x = this.DynamicCommands;
            this.LoadPluginIcons();

            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;
            _ = Task.Run(() => ConnectAsync(token), token);
        }

        public override void Unload()
        {
            _cancellationTokenSource.Cancel();
            Client.Disconnect();
        }
        
        public override void RunCommand(string commandName, string parameter)
        {
        }

        public override void ApplyAdjustment(string adjustmentName, string parameter, int diff)
        {
        }

        private void LoadPluginIcons()
        {
            //var resources = this.Assembly.GetManifestResourceNames();
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-256.png");
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!Client.IsConnected)
                        await ConnectAndSetStatusAsync();

                    await UpdateStatesAsync();
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }

        private async Task ConnectAndSetStatusAsync()
        {
            var connected = await Client.ConnectAsync();
            if (connected)
                return;

            base.OnPluginStatusChanged(
                Loupedeck.PluginStatus.Warning,
                "Could not connect to WaveLink.",
                "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/",
                "Plugin GitHub page");
        }

        private async Task UpdateStatesAsync()
        {
            var monitoringState = await Client.GetMonitoringState();
            Client.OutputMixerChanged?.Invoke(this, monitoringState);

            var mixOutputList = await Client.GetMonitorMixOutputList();
            LocalMonitorOutputFetched?.Invoke(this, mixOutputList?.MonitorMixList);
            Client.LocalMonitorOutputChanged?.Invoke(this, mixOutputList?.MonitorMix);

            var inputMixes = await Client.GetAllChannelInfo();
            AllChannelInfoFetched?.Invoke(this, inputMixes);
        }
    }
}