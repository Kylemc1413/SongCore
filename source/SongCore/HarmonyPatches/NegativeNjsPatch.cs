using SiraUtil.Affinity;

namespace SongCore.HarmonyPatches
{
    internal class AllowNegativeNjsValuesPatch : IAffinity
    {
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly BeatmapKey _beatmapKey;

        private AllowNegativeNjsValuesPatch(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapKey beatmapKey)
        {
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _beatmapKey = beatmapKey;
        }

        [AffinityPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.Init))]
        [AffinityPrefix]
        private void ForceNegativeStartNoteJumpMovementSpeed(ref float startNoteJumpMovementSpeed)
        {
            var noteJumpMovementSpeed = _gameplayCoreSceneSetupData.beatmapLevel.beatmapBasicData[(_beatmapKey.beatmapCharacteristic, _beatmapKey.difficulty)].noteJumpMovementSpeed;
            if (noteJumpMovementSpeed < 0)
            {
                startNoteJumpMovementSpeed = noteJumpMovementSpeed;
            }
        }
    }
}