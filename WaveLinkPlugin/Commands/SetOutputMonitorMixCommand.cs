using System.Collections.Generic;
using System.Linq;
using ElgatoWaveSDK;
using ElgatoWaveSDK.Models;

namespace Loupedeck.WaveLinkPlugin.Commands
{
    class SetOutputMonitorMixCommand : PluginDynamicCommand
    {
        private WaveLinkPlugin _plugin;
        private ElgatoWaveClient _client;

        private string _monitorMix;
        
        protected override bool OnLoad()
        {
            _plugin = (WaveLinkPlugin)base.Plugin;
            _plugin.LocalMonitorOutputFetched += LocalMonitorOutputFetched;

            _client = _plugin.Client;
            _client.LocalMonitorOutputChanged += LocalMonitorOutputChanged;

            return true;
        }

        protected override bool OnUnload()
        {
            _plugin.LocalMonitorOutputFetched -= LocalMonitorOutputFetched;

            _client.LocalMonitorOutputChanged -= LocalMonitorOutputChanged;

            return true;
        }

        private void LocalMonitorOutputFetched(object sender, IEnumerable<MonitorMixList> monitorMixList)
        {
            if (monitorMixList is null)
                return;
            
            var parameters = base.GetParameters()
                .Select(parameter => parameter.Name)
                .ToArray();

            foreach (var monitorMix in monitorMixList)
            {
                var monitorMixName = monitorMix.MonitorMix;
                if (string.IsNullOrWhiteSpace(monitorMixName))
                    continue;

                if (parameters.Contains(monitorMixName))
                    continue;

                base.AddParameter(monitorMixName, monitorMixName, "Output Destination");
            }
        }

        private void LocalMonitorOutputChanged(object sender, string monitorMix)
        {
            if (monitorMix is null)
                return;

            if (_monitorMix == monitorMix)
                return;

            _monitorMix = monitorMix;
            
            base.ActionImageChanged();
        }
        
        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter is null || !_client.IsConnected)
                return;
            
            _monitorMix = _client.SetMonitorMixOutput(actionParameter)
                .GetAwaiter().GetResult()
                ?.MonitorMix;

            base.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter is null || _client.IsConnected != true)
                return base.GetCommandImage(actionParameter, imageSize);
            
            var selected = _monitorMix == actionParameter;

            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                if (selected)
                    bitmapBuilder.Clear(new BitmapColor(0x00, 0x4E, 0x00));
                else
                    bitmapBuilder.Clear(new BitmapColor(0x4E, 0x00, 0x00));
                
                bitmapBuilder.DrawText(actionParameter);

                return bitmapBuilder.ToImage();
            }
        }
    }
}
