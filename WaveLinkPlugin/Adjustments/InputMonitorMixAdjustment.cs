using System;
using System.Collections.Generic;
using System.Linq;
using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Adjustments
{
    class InputMonitorMixAdjustment : PluginDynamicAdjustment
    {
        private WaveLinkPlugin _plugin;
        private ElgatoWaveClient _client;

        private readonly Dictionary<string, ChannelInfo> _states;

        public InputMonitorMixAdjustment()
            : base(true)
        {
            this.DisplayName = "Input Monitor Volume";
            this.GroupName = "";
            this.Description = "Input Monitor Volume and mute";

            this.MakeProfileAction("list;Input:");

            _states = new Dictionary<string, ChannelInfo>();
        }

        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;
            _client = _plugin.Client;

            _client.InputMixerChanged += InputMixerChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _client.InputMixerChanged -= InputMixerChanged;

            return true;
        }

        private void InputMixerChanged(object sender, ChannelInfo channelInfo)
        {
            if (channelInfo?.MixId is null)
                return;

            _states[channelInfo.MixId] = channelInfo;
        }

        protected override PluginActionParameter[] GetParameters()
        {
            if (!_client.IsConnected)
                return Array.Empty<PluginActionParameter>();

            return _states.Values
                .Select(channelInfo => new PluginActionParameter($"inputMonitor|{channelInfo.MixId}", channelInfo.MixerName, string.Empty))
                .ToArray();
        }


        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter is null || !_client.IsConnected)
                return;

            var mixId = actionParameter.Split('|')[1];

            if(!_states.TryGetValue(mixId, out var inputMix))
                return;

            inputMix.IsLocalInMuted = !inputMix.IsLocalInMuted;
            inputMix.IsLocalInMuted = _client.SetInputMixer(inputMix, MixType.LocalMix)
                .GetAwaiter()
                .GetResult()
                ?.IsLocalInMuted;

            base.ActionImageChanged();
        }

        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            if (actionParameter is null || _states is null)
                return;

            var mixId = actionParameter.Split('|')[1];
            if (!_states.TryGetValue(mixId, out var inputMix))
                return;

            var volume = inputMix.LocalVolumeIn;

            volume += diff;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            inputMix.LocalVolumeIn = volume;
            inputMix.LocalVolumeIn = _client.SetInputMixer(inputMix, MixType.LocalMix)
                .GetAwaiter().GetResult()
                ?.LocalVolumeIn;

            base.AdjustmentValueChanged(actionParameter);
        }

        protected override string GetAdjustmentValue(string actionParameter)
        {
            if (actionParameter is null || _states is null)
                return "-";

            var mixId = actionParameter.Split('|')[1];
            if (!_states.TryGetValue(mixId, out var inputMix))
                return "-";

            if (inputMix.IsLocalInMuted is true)
                return "muted";

            var volume = inputMix.LocalVolumeIn;

            return $"{volume:0}";
        }
    }
}