using System.Linq;
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
            var sceneSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
            var mapNjs = sceneSetupData.beatmapLevel.beatmapBasicData.First(p => p.Key == (sceneSetupData.beatmapKey.beatmapCharacteristic, sceneSetupData.beatmapKey.difficulty)).Value.noteJumpMovementSpeed;
            if (mapNjs < 0)
                startNoteJumpMovementSpeed = mapNjs;
        }
    }
}