using HarmonyLib;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.Init), typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings),
        typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool))]
    [HarmonyPatch("Init", MethodType.Normal)]
    internal class SceneTransitionPatch
    {
        private static void Prefix(ref IDifficultyBeatmap difficultyBeatmap, ref ColorScheme? overrideColorScheme)
        {
            var environmentInfoSO = difficultyBeatmap.GetEnvironmentInfo();
            var fallbackScheme = new ColorScheme(environmentInfoSO.colorScheme);

            var songData = Collections.RetrieveDifficultyData(difficultyBeatmap);
            if (songData == null)
            {
                return;
            }

            if (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null && songData._envColorRight == null && songData._obstacleColor == null)
            {
                return;
            }

            if (Plugin.CustomSongColors)
            {
                Logging.Logger.Info("Custom Song Colors On");
                var saberLeft = songData._colorLeft == null ? fallbackScheme.saberAColor : Utils.ColorFromMapColor(songData._colorLeft);
                var saberRight = songData._colorRight == null ? fallbackScheme.saberBColor : Utils.ColorFromMapColor(songData._colorRight);
                var envLeft = songData._envColorLeft == null
                    ? songData._colorLeft == null ? fallbackScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft)
                    : Utils.ColorFromMapColor(songData._envColorLeft);
                var envRight = songData._envColorRight == null
                    ? songData._colorRight == null ? fallbackScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight)
                    : Utils.ColorFromMapColor(songData._envColorRight);
                var envLeftBoost = songData._envColorLeftBoost == null ? envLeft : Utils.ColorFromMapColor(songData._envColorLeftBoost);
                var envRightBoost = songData._envColorRightBoost == null ? envRight : Utils.ColorFromMapColor(songData._envColorRightBoost);
                var obstacle = songData._obstacleColor == null ? fallbackScheme.obstaclesColor : Utils.ColorFromMapColor(songData._obstacleColor);
                overrideColorScheme = overrideColorScheme ?? new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft,
                    envRight, true, envLeftBoost, envRightBoost, obstacle);
            }
        }
    }
}
