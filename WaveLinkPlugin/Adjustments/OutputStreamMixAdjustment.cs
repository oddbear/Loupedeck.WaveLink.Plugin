using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Adjustments
{
    class OutputStreamMixAdjustment : PluginDynamicAdjustment
    {
        private WaveLinkPlugin _plugin;
        private WaveLinkClient _client;

        private MonitoringState _state;

        public OutputStreamMixAdjustment()
            : base("Stream Mix Volume", "Adjustment for Stream Mix Output Volume and Mute", "Output Volume", true)
        {
            //
        }

        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;
            _client = _plugin.Client;

            _client.OutputMixerChanged += OutputMixerChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _client.OutputMixerChanged -= OutputMixerChanged;

            return true;
        }

        private void OutputMixerChanged(object sender, MonitoringState state)
        {
            if (state is null)
                return;

            _state = state;

            base.AdjustmentValueChanged();
        }

        protected override void RunCommand(string actionParameter)
        {
            if (_state is null)
                return;

            _state.IsStreamOutMuted = !_state.IsStreamOutMuted;
            var result = _client.SetOutputMixer(_state)
                ?.IsStreamOutMuted;

            if (result != null)
                _state.IsStreamOutMuted = result;

            base.ActionImageChanged(actionParameter);
        }
        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            if (_state is null)
                return;

            var volume = _state.StreamVolumeOut;

            volume += diff;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            _state.StreamVolumeOut = volume;
            var result = _client.SetOutputMixer(_state)
                ?.StreamVolumeOut;

            if (result != null)
                _state.StreamVolumeOut = result;

            base.AdjustmentValueChanged(actionParameter);
        }

        protected override string GetAdjustmentValue(string actionParameter)
        {
            if (_state is null)
                return "-";

            if (_state.IsStreamOutMuted is true)
                return "muted";

            var volume = _state.StreamVolumeOut;

            return $"{volume:0.00}";
        }
    }
}
