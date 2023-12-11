using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using SongCore.Utilities;
using Utils = SongCore.Utilities.Utils;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch]
    internal class SceneTransitionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.Init),
                new[]
                {
                    typeof(string),
                    typeof(IDifficultyBeatmap),
                    typeof(IPreviewBeatmapLevel),
                    typeof(OverrideEnvironmentSettings),
                    typeof(ColorScheme),
                    typeof(ColorScheme),
                    typeof(GameplayModifiers),
                    typeof(PlayerSpecificSettings),
                    typeof(PracticeSettings),
                    typeof(string),
                    typeof(bool),
                    typeof(bool),
                    typeof(BeatmapDataCache),
                    typeof(RecordingToolManager.SetupData?)
                });

            yield return AccessTools.Method(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.Init),
                new[]
                {
                    typeof(string),
                    typeof(IPreviewBeatmapLevel),
                    typeof(BeatmapDifficulty),
                    typeof(BeatmapCharacteristicSO),
                    typeof(IDifficultyBeatmap),
                    typeof(ColorScheme),
                    typeof(GameplayModifiers),
                    typeof(PlayerSpecificSettings),
                    typeof(PracticeSettings),
                    typeof(bool)
                });
        }

        private static void Prefix(ref IDifficultyBeatmap difficultyBeatmap, ref ColorScheme? overrideColorScheme)
        {
            if (difficultyBeatmap == null || !Plugin.Configuration.CustomSongNoteColors && !Plugin.Configuration.CustomSongEnvironmentColors && !Plugin.Configuration.CustomSongObstacleColors)
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

            if (Plugin.Configuration.CustomSongNoteColors) Logging.Logger.Info("Custom Song Note Colors On");
            if (Plugin.Configuration.CustomSongEnvironmentColors) Logging.Logger.Info("Custom Song Environment Colors On");
            if (Plugin.Configuration.CustomSongObstacleColors) Logging.Logger.Info("Custom Song Obstacle Colors On");

            var saberLeft = (songData._colorLeft == null || !Plugin.Configuration.CustomSongNoteColors)
                ? fallbackScheme.saberAColor
                : Utils.ColorFromMapColor(songData._colorLeft);
            var saberRight = (songData._colorRight == null || !Plugin.Configuration.CustomSongNoteColors)
                ? fallbackScheme.saberBColor
                : Utils.ColorFromMapColor(songData._colorRight);
            var envLeft = (songData._envColorLeft == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? songData._colorLeft == null ? fallbackScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            var envRight = (songData._envColorRight == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? songData._colorRight == null ? fallbackScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            var envWhite = (songData._envColorWhite == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? fallbackScheme.environmentColorW
                : Utils.ColorFromMapColor(songData._envColorWhite);
            var envLeftBoost = (songData._envColorLeftBoost == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? fallbackScheme.environmentColor0Boost
                : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = (songData._envColorRightBoost == null|| !Plugin.Configuration.CustomSongEnvironmentColors)
                ? fallbackScheme.environmentColor1Boost
                : Utils.ColorFromMapColor(songData._envColorRightBoost);
            var envWhiteBoost = (songData._envColorWhiteBoost == null  || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? fallbackScheme.environmentColorWBoost
                : Utils.ColorFromMapColor(songData._envColorWhiteBoost);
            var obstacle = (songData._obstacleColor == null || !Plugin.Configuration.CustomSongObstacleColors)
                ? fallbackScheme.obstaclesColor
                : Utils.ColorFromMapColor(songData._obstacleColor);
            overrideColorScheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft,
                envRight, envWhite, true, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
            overrideColorScheme._environmentColorW = envWhite;
            overrideColorScheme._environmentColorWBoost = envWhiteBoost;
        }
    }
}
