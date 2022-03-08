using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData))]
    [HarmonyPatch(nameof(BeatmapObjectSpawnMovementData.Init), MethodType.Normal)]
    internal class AllowNegativeNjsValuesPatch
    {
        private static void Prefix(ref float startNoteJumpMovementSpeed)
        {
            if (!BS_Utils.Plugin.LevelData.IsSet) return;
            var mapNjs = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpMovementSpeed;
            if (mapNjs < 0)
                startNoteJumpMovementSpeed = mapNjs;
        }
    }
}
