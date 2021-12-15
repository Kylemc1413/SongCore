using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapDifficultyMethods))]
    [HarmonyPatch(nameof(BeatmapDifficultyMethods.Name), MethodType.Normal)]
    internal class BeatmapDifficultyMethodsName
    {
        private static void Postfix(BeatmapDifficulty difficulty, ref string __result)
        {
            if (!Plugin.Configuration.DisplayDiffLabels)
            {
                return;
            }

            __result = (difficulty switch
                {
                    BeatmapDifficulty.Easy when StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride != null => StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride,
                    BeatmapDifficulty.Normal when StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride != null => StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride,
                    BeatmapDifficulty.Hard when StandardLevelDetailViewRefreshContent.currentLabels.HardOverride != null => StandardLevelDetailViewRefreshContent.currentLabels.HardOverride,
                    BeatmapDifficulty.Expert when StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride != null => StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride,
                    BeatmapDifficulty.ExpertPlus when StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride != null => StandardLevelDetailViewRefreshContent.currentLabels
                        .ExpertPlusOverride,
                    _ => __result
                })
                .Replace(@"<", "<\u200B")
                .Replace(@">", ">\u200B");
        }
    }
}