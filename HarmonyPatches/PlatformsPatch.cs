using Harmony;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches {

    [HarmonyPatch(typeof(LevelSelectionFlowCoordinator))]
    [HarmonyPatch("StartLevel")]
    class LevelSelectionFlowCoordinatorStartLevelPatch  {
        static void Prefix(IDifficultyBeatmap difficultyBeatmap) {
            Data.ExtraSongData.DifficultyData songData = Collections.RetrieveDifficultyData(difficultyBeatmap);
            if(songData != null) {
                if(Plugin.PlatformsInstalled) {
                    Logging.logger.Info("Checking Custom Environment before song is loaded");
                    Plugin.CheckCustomSongEnvironment(difficultyBeatmap);
                }
            } else {
                Logging.logger.Info("Null custom song extra data");
            }
        }
    }
}
