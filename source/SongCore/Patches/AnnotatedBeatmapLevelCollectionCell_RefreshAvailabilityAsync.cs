using System;
using HarmonyLib;

/*
 * This patch removes the download icon for empty beatmaplevelcollections
 * Introduced since 1.18.0
 */

namespace SongCore.Patches
{
    [HarmonyPatch(typeof(AnnotatedBeatmapLevelCollectionCell), nameof(AnnotatedBeatmapLevelCollectionCell.RefreshAvailabilityAsync))]
    internal static class AnnotatedBeatmapLevelCollectionCell_RefreshAvailabilityAsync
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
