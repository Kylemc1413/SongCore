using SiraUtil.Affinity;

namespace SongCore.HarmonyPatches
{
    internal class AllowNegativeNjsValuesPatch : IAffinity
    {
        private readonly BeatmapBasicData _beatmapBasicData;

        private AllowNegativeNjsValuesPatch(BeatmapBasicData beatmapBasicData)
        {
            _beatmapBasicData = beatmapBasicData;
        }

        [AffinityPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.Init))]
        [AffinityPrefix]
        private void ForceNegativeStartNoteJumpMovementSpeed(ref float startNoteJumpMovementSpeed)
        {
            var noteJumpMovementSpeed = _beatmapBasicData.noteJumpMovementSpeed;
            if (noteJumpMovementSpeed < 0)
            {
                startNoteJumpMovementSpeed = noteJumpMovementSpeed;
            }
        }
    }
}
