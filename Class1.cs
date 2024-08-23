using Life;
using ModKit.Helper;
using ModKit.Interfaces;

namespace BetterCraft
{
    public class BetterCraft : ModKit.ModKit
    {
        public BetterCraft(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }
    }
}
