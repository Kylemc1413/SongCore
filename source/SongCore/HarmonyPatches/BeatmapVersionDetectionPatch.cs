using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch reverses base game logic so it first looks for V3 format instead of V2. Without this patch, maps that declare both <c>version</c> and <c>_version</c> will be empty.
    /// </summary>
    // TODO: Remove when fixed.
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers.VersionSerializedData), nameof(BeatmapSaveDataHelpers.VersionSerializedData.v), MethodType.Getter)]
    internal static class BeatmapVersionDetectionPatch
    {
        private static bool Prefix(BeatmapSaveDataHelpers.VersionSerializedData __instance, ref string __result)
        {
            __result = !string.IsNullOrEmpty(__instance.version) ? __instance.version : __instance._version;

            return false;
        }
    }
}
