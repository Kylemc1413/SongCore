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
                    BeatmapDifficulty.Easy when StandardLevelDetailViewRefreshContentPatch.currentLabels.EasyOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.EasyOverride,
                    BeatmapDifficulty.Normal when StandardLevelDetailViewRefreshContentPatch.currentLabels.NormalOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.NormalOverride,
                    BeatmapDifficulty.Hard when StandardLevelDetailViewRefreshContentPatch.currentLabels.HardOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.HardOverride,
                    BeatmapDifficulty.Expert when StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertOverride,
                    BeatmapDifficulty.ExpertPlus when StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertPlusOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels
                        .ExpertPlusOverride,
                    _ => __result
                })
                .Replace(@"<", "<\u200B")
                .Replace(@">", ">\u200B");
        }
    }
}