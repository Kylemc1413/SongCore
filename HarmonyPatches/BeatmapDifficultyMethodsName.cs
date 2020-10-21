using HarmonyLib;
namespace SongCore.HarmonyPatches
{


    [HarmonyPatch(typeof(BeatmapDifficultyMethods))]
    [HarmonyPatch("Name", MethodType.Normal)]
    public class BeatmapDifficultyMethodsName
    {
        static void Postfix(BeatmapDifficulty difficulty, ref string __result)
        {
            if (!Plugin.displayDiffLabels) return;
            if (difficulty == BeatmapDifficulty.Easy)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride != "")
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
            }
            if (difficulty == BeatmapDifficulty.Normal)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride != "")
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
            }
            if (difficulty == BeatmapDifficulty.Hard)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.HardOverride != "")
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.HardOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
            }
            if (difficulty == BeatmapDifficulty.Expert)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride != "")
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
            }
            if (difficulty == BeatmapDifficulty.ExpertPlus)
            {
                if (StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride != "")
                    __result = StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride.Replace(@"<", "<\u200B").Replace(@">", ">\u200B");
            }
            //    Console.WriteLine(__result);
        }




    }
}
