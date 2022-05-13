using HarmonyLib;
using IPA.Utilities;
using SongCore.Utilities;
using Utils = SongCore.Utilities.Utils;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO))]
    [HarmonyPatch("Init", MethodType.Normal)]
    internal class SceneTransitionPatch
    {
        private static void Prefix(ref IDifficultyBeatmap difficultyBeatmap, ref ColorScheme? overrideColorScheme)
        {
            if (!Plugin.Configuration.CustomSongColors)
            {
                return;
            }

            var songData = Collections.RetrieveDifficultyData(difficultyBeatmap);
            if (songData == null)
            {
                return;
            }

            if (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null && songData._envColorRight == null && songData._envColorWhite == null && songData._obstacleColor == null && songData._envColorLeftBoost == null && songData._envColorRightBoost == null && songData._envColorWhiteBoost == null)
            {
                return;
            }

            var environmentInfoSO = difficultyBeatmap.GetEnvironmentInfo();
            var fallbackScheme = overrideColorScheme ?? new ColorScheme(environmentInfoSO.colorScheme);

            Logging.Logger.Info("Custom Song Colors On");
            var saberLeft = songData._colorLeft == null ? fallbackScheme.saberAColor : Utils.ColorFromMapColor(songData._colorLeft);
            var saberRight = songData._colorRight == null ? fallbackScheme.saberBColor : Utils.ColorFromMapColor(songData._colorRight);
            var envLeft = songData._envColorLeft == null
                ? songData._colorLeft == null ? fallbackScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            var envRight = songData._envColorRight == null
                ? songData._colorRight == null ? fallbackScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            var envWhite = songData._envColorWhite == null ? fallbackScheme.environmentColorW : Utils.ColorFromMapColor(songData._envColorWhite);
            var envLeftBoost = songData._envColorLeftBoost == null ? envLeft : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = songData._envColorRightBoost == null ? envRight : Utils.ColorFromMapColor(songData._envColorRightBoost);
            var envWhiteBoost = songData._envColorWhiteBoost == null ? fallbackScheme.environmentColorWBoost : Utils.ColorFromMapColor(songData._envColorWhiteBoost);
            var obstacle = songData._obstacleColor == null ? fallbackScheme.obstaclesColor : Utils.ColorFromMapColor(songData._obstacleColor);
            overrideColorScheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft,
                envRight, true, envLeftBoost, envRightBoost, obstacle);
            overrideColorScheme.SetField("_environmentColorW", envWhite);
            overrideColorScheme.SetField("_environmentColorWBoost", envWhiteBoost);
        }
    }
}