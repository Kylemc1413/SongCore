using SiraUtil.Affinity;
using SongCore.Utilities;

namespace SongCore.Patches
{
    internal class SongDataGamePatches  : IAffinity
    {
        private readonly bool? _showRotationNoteSpawnLines;
        private readonly bool? _oneSaber;

        private SongDataGamePatches(BeatmapKey beatmapKey)
        {
            var difficultyData = Collections.GetCustomLevelSongDifficultyData(beatmapKey);
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
