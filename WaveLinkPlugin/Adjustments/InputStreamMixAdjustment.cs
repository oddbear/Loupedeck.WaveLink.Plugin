using System;
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
            this.DisplayName = "Input Stream Volume";
            this.GroupName = "";
            this.Description = "Input Stream Volume and mute";

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
                .Select(input => new PluginActionParameter($"inputStream|{input.MixId}", input.MixerName, string.Empty))
                .ToArray();
        }

        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter is null || !_client.IsConnected)
                return;

            var mixId = actionParameter.Split('|')[1];

            if (!_states.TryGetValue(mixId, out var inputMix))
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

            var mixId = actionParameter.Split('|')[1];
            if (!_states.TryGetValue(mixId, out var inputMix))
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
