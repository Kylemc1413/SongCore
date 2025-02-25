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

        [AffinityPatch(typeof(SaberManager), nameof(SaberManager.Start))]
        [AffinityPrefix]
        private void ForceOneSaber(SaberManager __instance)
        {
            if (_oneSaber.HasValue)
            {
                Accessors.SaberManagerInitDataAccessor(ref __instance) = new SaberManager.InitData(_oneSaber.Value, __instance._initData.oneSaberType);
            }
        }
    }
}
