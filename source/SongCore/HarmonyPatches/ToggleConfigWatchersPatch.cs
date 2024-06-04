using HarmonyLib;
using IPA.Config;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch disables BSIPA's monitoring of config files in the game scene.
    /// It leaks a lot of memory when there's a ton of files to watch.
    /// </summary>
    [HarmonyPatch(typeof(DisableGCWhileEnabled))]
    internal class ToggleConfigWatchersPatch
    {
        [HarmonyPatch(nameof(DisableGCWhileEnabled.OnEnable))]
        private static void Prefix()
        {
            ConfigWatchersHelper.ToggleWatchers();
        }

        [HarmonyPatch(nameof(DisableGCWhileEnabled.OnDisable))]
        private static void Postfix()
        {
            ConfigWatchersHelper.ToggleWatchers();
        }
    }
}
