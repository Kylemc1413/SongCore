using SiraUtil.Affinity;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    internal class BeatmapLevelDifficultyDataPatches  : IAffinity
    {
        private readonly bool? _showRotationNoteSpawnLines;
        private readonly bool? _oneSaber;

        private BeatmapLevelDifficultyDataPatches(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
        {
            var difficultyData = Collections.RetrieveDifficultyData(beatmapLevel, beatmapKey);
            if (difficultyData != null)
            {
                _showRotationNoteSpawnLines = difficultyData._showRotationNoteSpawnLines;
                _oneSaber = difficultyData._oneSaber;
            }
        }

        [AffinityPatch(typeof(BeatLineManager), nameof(BeatLineManager.HandleNoteWasSpawned))]
        [AffinityPrefix]
        private bool ShowOrHideRotationNoteSpawnLines()
        {
            return _showRotationNoteSpawnLines ?? true;
        }

        [AffinityPatch(typeof(SaberManager.InitData), "ctor", AffinityMethodType.Constructor, null, typeof(bool), typeof(SaberType))]
        private void ForceOneSaber(SaberManager.InitData __instance)
        {
            if (_oneSaber.HasValue)
            {
                Accessors.OneSaberModeAccessor(ref __instance) = _oneSaber.Value;
            }
        }
    }
}
