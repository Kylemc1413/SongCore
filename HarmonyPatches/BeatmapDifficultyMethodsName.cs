using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
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
                if(HarmonyPatches.StandardLevelDetailViewRefreshContent.EasyOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.EasyOverride;
            }
            if (difficulty == BeatmapDifficulty.Normal)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.NormalOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.NormalOverride;
            }
            if (difficulty == BeatmapDifficulty.Hard)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.HardOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.HardOverride;
            }
            if (difficulty == BeatmapDifficulty.Expert)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.ExpertOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.ExpertOverride;
            }
            if (difficulty == BeatmapDifficulty.ExpertPlus)
            {
                if (HarmonyPatches.StandardLevelDetailViewRefreshContent.ExpertPlusOverride != "")
                    __result = HarmonyPatches.StandardLevelDetailViewRefreshContent.ExpertPlusOverride;
            }
        //    Console.WriteLine(__result);
        }




    }
}
