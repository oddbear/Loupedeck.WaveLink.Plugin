namespace Loupedeck.WaveLinkPlugin
{
    using System;
    using System.Threading.Tasks;

    using ElgatoWaveSDK;

    public class WaveLinkPlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        public ElgatoWaveClient Client;

        public override void Load()
        {
            this.LoadPluginIcons();

            Client = new ElgatoWaveClient();
            Client.ConnectAsync(); //true false, connected or not.
        }

        public override void Unload()
        {
        }

        private void OnApplicationStarted(Object sender, EventArgs e)
        {
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
        }

        public override void RunCommand(String commandName, String parameter)
        {
        }

        public override void ApplyAdjustment(String adjustmentName, String parameter, Int32 diff)
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
