namespace Loupedeck.WaveLinkPlugin.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class TestCommand : PluginDynamicCommand
    {
        private WaveLinkPlugin _plugin;
        private long? _localVolume;

        public TestCommand()
            : base("Input Volume", "Gets the input volume", "TestWave")
        {
            //
        }

        protected override Boolean OnLoad()
        {
            this._plugin = (WaveLinkPlugin)base.Plugin;
            Task.Run(() =>
            {
                while (this._plugin.Client is null)
                {
                    Thread.Sleep(200);
                }

                _plugin.Client.InputMixerChanged += (sender, c) =>
                {
                    _localVolume = c.LocalVolumeIn;
                    base.ActionImageChanged();
                };
            });
            return true;
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return this._localVolume?.ToString() ?? "-";
        }
    }
}