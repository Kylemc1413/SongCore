using HarmonyLib;

/*
 * This patch removes the download icon for empty beatmaplevelcollections
 * Introduced since 1.18.0
 */

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(AnnotatedBeatmapLevelCollectionCell), nameof(AnnotatedBeatmapLevelCollectionCell.RefreshAvailabilityAsync))]
    internal class AnnotatedBeatmapLevelCollectionCell_RefreshAvailabilityAsync
    {
        private static void Postfix(AnnotatedBeatmapLevelCollectionCell __instance, IAnnotatedBeatmapLevelCollection ____annotatedBeatmapLevelCollection)
        {
            if (____annotatedBeatmapLevelCollection is CustomBeatmapLevelPack)
            {
                __instance.SetDownloadIconVisible(false);
            }
        }
    }
}
