using HarmonyLib;
using IPA.Config;

namespace SongCore.Patches
{
    /// <summary>
    /// This patch temporarily disables BSIPA's monitoring of config files while garbage collection is disabled, to avoid excessive memory allocation.
    /// </summary>
    [HarmonyPatch(typeof(DisableGCWhileEnabled))]
    internal static class ToggleConfigWatchersPatch
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
