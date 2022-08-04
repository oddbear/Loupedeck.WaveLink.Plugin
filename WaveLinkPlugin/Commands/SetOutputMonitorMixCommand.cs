using System;
using System.Linq;
using ElgatoWaveSDK;

namespace Loupedeck.WaveLinkPlugin.Commands
{
    class SetOutputMonitorMixCommand : PluginDynamicCommand
    {
        private WaveLinkPlugin _plugin;
        private ElgatoWaveClient _client;

        private string _monitorMix;

        public SetOutputMonitorMixCommand()
            : base()
        {
            this.DisplayName = "Switch Monitor Mix";
            this.GroupName = "";
            this.Description = "Switch Monitor Mix Output";

            this.MakeProfileAction("list;MonitorMix:");
        }

        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;
            _client = _plugin.Client;

            _client.LocalMonitorOutputChanged += LocalMonitorOutputChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _client.LocalMonitorOutputChanged -= LocalMonitorOutputChanged;

            return true;
        }

        private void LocalMonitorOutputChanged(object sender, string monitorMix)
        {
            _monitorMix = monitorMix;

            //Needs to update all of them...
            base.ActionImageChanged();
        }
        
        //------------------------------------
        // Command: Get/Set Monitor mix
        //------------------------------------
        //var mixType = await client.GetSwitchState();
        //client.SetMonitoringState(MixType.LocalMix); //Set listen (ear) to local mix
        //client.SetMonitoringState(MixType.StreamMix); //Set listen (ear) to stream mix

        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter is null || !_client.IsConnected)
                return;

            var monitorMix = actionParameter.Split('|')[1];
            _monitorMix = _client.SetMonitorMixOutput(monitorMix)
                .GetAwaiter().GetResult()
                .MonitorMix;

            base.ActionImageChanged();
        }

        protected override PluginActionParameter[] GetParameters()
        {
            if (!_client.IsConnected)
                return Array.Empty<PluginActionParameter>();

            //TODO: I don't need to fetch this every time I think:
            var monitorMixes = _client.GetMonitorMixOutputList()
                .GetAwaiter()
                .GetResult()
                //.MonitorMix //This is the selected one as string.
                ?.MonitorMixList;
            
            if (monitorMixes is null)
                return Array.Empty<PluginActionParameter>();

            return monitorMixes
                .Select(monitorMix => monitorMix.MonitorMix)
                .Select(monitorMix => new PluginActionParameter($"monitorMix|{monitorMix}", monitorMix, string.Empty))
                .ToArray();
        }
        
        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter is null || _client.IsConnected != true)
                return base.GetCommandImage(actionParameter, imageSize);

            var monitorMix = actionParameter.Split('|')[1];
            var selected = _monitorMix == monitorMix;

            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                if (selected)
                    bitmapBuilder.Clear(new BitmapColor(0x00, 0x4E, 0x00));
                else
                    bitmapBuilder.Clear(new BitmapColor(0x4E, 0x00, 0x00));

                var text = base.GetCommandDisplayName(actionParameter, imageSize);
                bitmapBuilder.DrawText(monitorMix);

                return bitmapBuilder.ToImage();
            }
        }
    }
}
