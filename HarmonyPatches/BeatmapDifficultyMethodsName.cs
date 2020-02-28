using HarmonyLib;
namespace SongCore.HarmonyPatches
{


    [HarmonyPatch(typeof(BeatmapDifficultyMethods))]
    [HarmonyPatch("Name", MethodType.Normal)]
    public class BeatmapDifficultyMethodsName
    {
        static void Postfix(BeatmapDifficulty difficulty, ref string __result)
        {
            if (difficulty == BeatmapDifficulty.Easy)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.EasyOverride;
            }
            if (difficulty == BeatmapDifficulty.Normal)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.NormalOverride;
            }
            if (difficulty == BeatmapDifficulty.Hard)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.HardOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.HardOverride;
            }
            if (difficulty == BeatmapDifficulty.Expert)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.ExpertOverride;
            }
            if (difficulty == BeatmapDifficulty.ExpertPlus)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.currentLabels.ExpertPlusOverride;
            }
            //    Console.WriteLine(__result);
        }




    }
}
