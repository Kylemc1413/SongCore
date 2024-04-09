using HarmonyLib;
using SongCore.Data;
using SongCore.Utilities;
using Utils = SongCore.Utilities.Utils;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch]
    internal class CustomSongColorsPatch
    {
        [HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo))]
        internal class StandardLevelScenesTransitionSetupDataPatch
        {
            private static void Postfix(StandardLevelScenesTransitionSetupDataSO __instance)
            {
                if (!Plugin.Configuration.CustomSongNoteColors && !Plugin.Configuration.CustomSongEnvironmentColors && !Plugin.Configuration.CustomSongObstacleColors)
                {
                    return;
                }
                var colorScheme = __instance.colorScheme;
                var songData = Collections.RetrieveDifficultyData(__instance.beatmapLevel, __instance.beatmapKey);
                var overrideColorScheme = GetOverrideColorScheme(songData, colorScheme);
                if (overrideColorScheme is null)
                {
                    return;
                }

                __instance.usingOverrideColorScheme = true;
                __instance.colorScheme = overrideColorScheme;
            }
        }

        [HarmonyPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.InitColorInfo))]
        internal class MultiplayerLevelScenesTransitionSetupDataPatch
        {
            private static void Postfix(MultiplayerLevelScenesTransitionSetupDataSO __instance)
            {
                if (!Plugin.Configuration.CustomSongNoteColors && !Plugin.Configuration.CustomSongEnvironmentColors && !Plugin.Configuration.CustomSongObstacleColors)
                {
                    return;
                }
                var colorScheme = __instance.colorScheme;
                var songData = Collections.RetrieveDifficultyData(__instance.beatmapLevel, __instance.beatmapKey);
                var overrideColorScheme = GetOverrideColorScheme(songData, colorScheme);
                if (overrideColorScheme is null)
                {
                    return;
                }

                __instance.usingOverrideColorScheme = true;
                __instance.colorScheme = overrideColorScheme;
            }
        }

        private static ColorScheme? GetOverrideColorScheme(ExtraSongData.DifficultyData? songData, ColorScheme currentColorScheme)
        {
            if (songData is null || (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null && songData._envColorRight == null &&
                                     songData._envColorWhite == null && songData._obstacleColor == null && songData._envColorLeftBoost == null && songData._envColorRightBoost == null &&
                                     songData._envColorWhiteBoost == null))
            {
                return null;
            }

            if (Plugin.Configuration.CustomSongNoteColors) Logging.Logger.Info("Custom song note colors On");
            if (Plugin.Configuration.CustomSongEnvironmentColors) Logging.Logger.Info("Custom song environment colors On");
            if (Plugin.Configuration.CustomSongObstacleColors) Logging.Logger.Info("Custom song obstacle colors On");

            var saberLeft = (songData._colorLeft == null || !Plugin.Configuration.CustomSongNoteColors)
                ? currentColorScheme.saberAColor
                : Utils.ColorFromMapColor(songData._colorLeft);
            var saberRight = (songData._colorRight == null || !Plugin.Configuration.CustomSongNoteColors)
                ? currentColorScheme.saberBColor
                : Utils.ColorFromMapColor(songData._colorRight);
            var envLeft = (songData._envColorLeft == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? songData._colorLeft == null ? currentColorScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            var envRight = (songData._envColorRight == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? songData._colorRight == null ? currentColorScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            var envWhite = (songData._envColorWhite == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? currentColorScheme.environmentColorW
                : Utils.ColorFromMapColor(songData._envColorWhite);
            var envLeftBoost = (songData._envColorLeftBoost == null || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? currentColorScheme.environmentColor0Boost
                : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = (songData._envColorRightBoost == null|| !Plugin.Configuration.CustomSongEnvironmentColors)
                ? currentColorScheme.environmentColor1Boost
                : Utils.ColorFromMapColor(songData._envColorRightBoost);
            var envWhiteBoost = (songData._envColorWhiteBoost == null  || !Plugin.Configuration.CustomSongEnvironmentColors)
                ? currentColorScheme.environmentColorWBoost
                : Utils.ColorFromMapColor(songData._envColorWhiteBoost);
            var obstacle = (songData._obstacleColor == null || !Plugin.Configuration.CustomSongObstacleColors)
                ? currentColorScheme.obstaclesColor
                : Utils.ColorFromMapColor(songData._obstacleColor);

            return new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft,
                envRight, envWhite, true, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
        }
    }
}
