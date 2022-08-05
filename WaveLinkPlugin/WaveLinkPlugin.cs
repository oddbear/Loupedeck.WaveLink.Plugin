using System;
using System.Threading.Tasks;
using ElgatoWaveSDK;

namespace Loupedeck.WaveLinkPlugin
{
    public class WaveLinkPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;
        
        public ElgatoWaveClient Client;

        public WaveLinkPlugin()
        {
            Client = new ElgatoWaveClient();
        }

        public override void Load()
        {
            this.LoadPluginIcons();
            
            _ = ConnectAsync();
        }
        
        private async Task ConnectAsync()
        {
            try
            {
                var connected = await Client.ConnectAsync();
                if (!connected)
                    base.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Could not connect to WaveLink.", "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/", "Plugin GitHub page");
                
                var monitoringState = await Client.GetMonitoringState();
                if (monitoringState != null)
                    Client.OutputMixerChanged?.Invoke(this, monitoringState);

                var mixOutputList = await Client.GetMonitorMixOutputList();
                if (mixOutputList?.MonitorMix != null)
                    Client.LocalMonitorOutputChanged?.Invoke(this, mixOutputList.MonitorMix);
                
                var inputMixes = await Client.GetAllChannelInfo();
                if (inputMixes != null)
                {
                    foreach (var channelInfo in inputMixes)
                    {
                        Client.InputMixerChanged?.Invoke(this, channelInfo);
                    }
                }

            }
            catch (Exception)
            {
                base.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "Could not connect to WaveLink.", "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/", "Plugin GitHub page");
            }
        }

        public override void Unload()
        {
            Client.Disconnect();
        }
        
        private void OnApplicationStarted(object sender, EventArgs e)
        {
        }

        private void OnApplicationStopped(object sender, EventArgs e)
        {
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
    }
}