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

            if (difficulty == BeatmapDifficulty.Easy)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride != null)
                {
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
                }
            }

            if (difficulty == BeatmapDifficulty.Normal)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride != null)
                {
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
                }
            }

            if (difficulty == BeatmapDifficulty.Hard)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.HardOverride != null)
                {
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.HardOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
                }
            }

            if (difficulty == BeatmapDifficulty.Expert)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride != null)
                {
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
                }
            }

            if (difficulty == BeatmapDifficulty.ExpertPlus)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride != null)
                {
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
                }
            }
        }
    }
}