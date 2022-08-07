using System.Collections.Generic;
using System.Linq;
using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Adjustments
{
    class InputStreamMixAdjustment : PluginDynamicAdjustment
    {
        private WaveLinkPlugin _plugin;
        private ElgatoWaveClient _client;

        private readonly Dictionary<string, ChannelInfo> _states;

        public InputStreamMixAdjustment()
            : base(true)
        {
            _states = new Dictionary<string, ChannelInfo>();
        }

        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;
            _plugin.AllChannelInfoFetched += PluginOnAllChannelInfoFetched;

            _client = _plugin.Client;
            _client.InputMixerChanged += InputMixerChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _plugin.AllChannelInfoFetched -= PluginOnAllChannelInfoFetched;

            _client.InputMixerChanged -= InputMixerChanged;

            return true;
        }

        private void PluginOnAllChannelInfoFetched(object sender, IEnumerable<ChannelInfo> channels)
        {
            if (channels is null)
                return;

            var parameters = base.GetParameters()
                .Select(parameter => parameter.Name)
                .ToArray();

            foreach (var channelInfo in channels)
            {
                if (channelInfo?.MixId is null)
                    continue;

                if (_states.ContainsKey(channelInfo.MixId))
                    continue;

                _states[channelInfo.MixId] = channelInfo;

                var monitorMixName = channelInfo.MixId;
                if (string.IsNullOrWhiteSpace(monitorMixName))
                    continue;

                if (parameters.Contains(monitorMixName))
                    continue;

                base.AddParameter(channelInfo.MixId, channelInfo.MixerName, "Input Volume (Stream)");
            }
        }

        private void InputMixerChanged(object sender, ChannelInfo channelInfo)
        {
            if (channelInfo?.MixId is null)
                return;

            _states[channelInfo.MixId] = channelInfo;

            base.ActionImageChanged(channelInfo.MixId);
        }
        
        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter is null || !_client.IsConnected)
                return;
            
            if (!_states.TryGetValue(actionParameter, out var inputMix))
                return;

            inputMix.IsStreamInMuted = !inputMix.IsStreamInMuted;
            inputMix.IsStreamInMuted = _client.SetInputMixer(inputMix, MixType.StreamMix)
                .GetAwaiter()
                .GetResult()
                ?.IsStreamInMuted;

            base.ActionImageChanged();
        }

        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            if (actionParameter is null || _states is null)
                return;
            
            if (!_states.TryGetValue(actionParameter, out var inputMix))
                return;

            var volume = inputMix.StreamVolumeIn;

            volume += diff;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            inputMix.StreamVolumeIn = volume;
            inputMix.StreamVolumeIn = _client.SetInputMixer(inputMix, MixType.StreamMix)
                .GetAwaiter().GetResult()
                ?.StreamVolumeIn;

            base.AdjustmentValueChanged(actionParameter);
        }

        protected override string GetAdjustmentValue(string actionParameter)
        {
            if (actionParameter is null || _states is null)
                return "-";
            
            if (!_states.TryGetValue(actionParameter, out var inputMix))
                return "-";

            if (inputMix.IsLocalInMuted is true)
                return "muted";

            var volume = inputMix.LocalVolumeIn;

            return $"{volume:0}";
        }
    }
}
