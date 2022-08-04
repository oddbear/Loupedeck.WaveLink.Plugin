using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Adjustments
{
    class OutputMonitorMixAdjustment : PluginDynamicAdjustment
    {
        private WaveLinkPlugin _plugin;
        private ElgatoWaveClient _client;

        private MonitoringState _state;
        
        public OutputMonitorMixAdjustment()
            : base("Monitor Mix Volume", "Adjustment for Monitor Mix Output Volume and Mute", "Outputs", true, DeviceType.All)
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
            _state = state;

            base.AdjustmentValueChanged();
        }

        protected override void RunCommand(string actionParameter)
        {
            if (_state is null)
                return;

            _state.IsLocalOutMuted = !_state.IsLocalOutMuted;
            _state.IsLocalOutMuted = _client.SetOutputMixer(_state)
                .GetAwaiter().GetResult()
                .IsLocalOutMuted;

            base.ActionImageChanged(actionParameter);
        }

        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            if (_state is null)
                return;

            var volume = _state.LocalVolumeOut;

            volume += diff;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            _state.LocalVolumeOut = volume;
            _state.LocalVolumeOut = _client.SetOutputMixer(_state)
                .GetAwaiter().GetResult()
                .LocalVolumeOut;
            
            base.AdjustmentValueChanged(actionParameter);
        }

        protected override string GetAdjustmentValue(string actionParameter)
        {
            if (_state is null)
                return "-";

            if (_state.IsLocalOutMuted is true)
                return "muted";

            var volume = _state.LocalVolumeOut;

            return $"{volume:0}";
        }
    }
}
