using System;
using HarmonyLib;

namespace SongCore.Patches
{
    /// <summary>
    /// This patch removes the download icon from empty custom annotated beatmap collections.
    /// </summary>
    [HarmonyPatch(typeof(AnnotatedBeatmapLevelCollectionCell), nameof(AnnotatedBeatmapLevelCollectionCell.RefreshAvailabilityAsync))]
    internal static class RemoveDownloadIconPatch
    {
        private static void Postfix(AnnotatedBeatmapLevelCollectionCell __instance)
        {
            if (__instance._beatmapLevelPack.packID.StartsWith(CustomLevelLoader.kCustomLevelPackPrefixId, StringComparison.Ordinal))
            {
                __instance.SetDownloadIconVisible(false);
            }
        }
    }
}
