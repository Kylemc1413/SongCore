using HarmonyLib;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches {

    /*
    [HarmonyPatch(typeof(LevelCollectionNavigationController))]
    [HarmonyPatch("HandleLevelDetailViewControllerDidPressActionButton")]
    class LevelSelectionFlowCoordinatorStartLevelPatch  {
        static void Prefix(StandardLevelDetailViewController viewController) {
            Data.ExtraSongData.DifficultyData songData = Collections.RetrieveDifficultyData(viewController.selectedDifficultyBeatmap);
            if(songData != null) {
                if(Plugin.customSongPlatforms) {
                    Logging.logger.Debug("Checking Custom Environment before song is loaded");
                    Plugin.CheckCustomSongEnvironment(viewController.selectedDifficultyBeatmap);
                }
            } else {
                Logging.logger.Debug("Null custom song extra data");
            }
        }
    }
    */
    
}
