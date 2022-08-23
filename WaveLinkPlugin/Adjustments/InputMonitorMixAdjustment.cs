using System.Collections.Generic;
using System.Linq;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Adjustments
{
    class InputMonitorMixAdjustment : PluginDynamicAdjustment
    {
        private WaveLinkPlugin _plugin;
        private WaveLinkClient _client;

        private readonly Dictionary<string, ChannelInfo> _states;

        public InputMonitorMixAdjustment()
            : base(true)
        {
            _states = new Dictionary<string, ChannelInfo>();
        }

        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;

            _client = _plugin.Client;
            _client.InputMixerChanged += InputMixerChanged;
            _client.ChannelsChanged += ChannelsChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _client.InputMixerChanged -= InputMixerChanged;
            _client.ChannelsChanged -= ChannelsChanged;

            return true;
        }
        
        private void ChannelsChanged(object sender, List<ChannelInfo> channels)
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

                base.AddParameter(channelInfo.MixId, channelInfo.MixerName, "Input Volume (Monitor)");
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
            
            if(!_states.TryGetValue(actionParameter, out var inputMix))
                return;

            inputMix.IsLocalInMuted = !inputMix.IsLocalInMuted;
            inputMix.IsLocalInMuted = _client.SetInputMixer(inputMix, MixType.LocalMix)
                ?.IsLocalInMuted;

            base.ActionImageChanged();
        }

        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            if (actionParameter is null || _states is null)
                return;
            
            if (!_states.TryGetValue(actionParameter, out var inputMix))
                return;

            var volume = inputMix.LocalVolumeIn;

            volume += diff;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            inputMix.LocalVolumeIn = volume;
            inputMix.LocalVolumeIn = _client.SetInputMixer(inputMix, MixType.LocalMix)
                ?.LocalVolumeIn;

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