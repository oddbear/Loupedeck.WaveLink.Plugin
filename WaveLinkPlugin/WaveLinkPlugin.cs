namespace Loupedeck.WaveLinkPlugin
{
    public class WaveLinkPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;
        
        internal WaveLinkClient Client;
        
        public WaveLinkPlugin()
        {
            Client = new WaveLinkClient(this);
        }

        public override void Load()
        {
            this.LoadPluginIcons();
            Client.Start();
        }

        public override void Unload()
        {
            Client.Dispose();
        }
        
        public override void RunCommand(string commandName, string parameter)
        {
        }

        public override void ApplyAdjustment(string adjustmentName, string parameter, int diff)
        {
        }

        private void LoadPluginIcons()
        {
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.WaveLinkPlugin.Resources.Icons.Icon-256.png");
        }
        
        internal void ConnectedStatus()
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal,
                "Connected.",
                "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/",
                "Plugin GitHub page");
        }

        internal void DisconnectedStatus()
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning,
                "Could not connect to WaveLink.",
                "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/",
                "Plugin GitHub page");
        }
        
        internal void ErrorStatus(string message)
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Error,
                $"Error: {message}",
                "https://github.com/oddbear/Loupedeck.WaveLink.Plugin/",
                "Plugin GitHub page");
        }
    }
}