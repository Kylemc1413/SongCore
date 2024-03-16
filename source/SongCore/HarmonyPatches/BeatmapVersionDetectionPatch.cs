using HarmonyLib;
using JetBrains.Annotations;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch fixes the base game implementation, which fails with maps that have no version declared.
    /// Without this patch affected maps don't load when selected.
    /// </summary>
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers.VersionSerializedData), nameof(BeatmapSaveDataHelpers.VersionSerializedData.v), MethodType.Getter)]
    internal static class BeatmapVersionDetectionPatch
    {
        [UsedImplicitly]
        private static void Postfix(ref string? __result)
        {
            __result ??= BeatmapSaveDataHelpers.noVersion.ToString();
        }
    }
}
