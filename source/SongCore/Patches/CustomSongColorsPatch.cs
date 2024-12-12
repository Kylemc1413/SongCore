using SiraUtil.Affinity;
using SongCore.Data;
using SongCore.Utilities;
using Utils = SongCore.Utilities.Utils;

namespace SongCore.Patches
{
    internal class CustomSongColorsPatches : IAffinity
    {
        private readonly PluginConfig _config;

        private CustomSongColorsPatches(PluginConfig config)
        {
            _config = config;
        }

        [AffinityPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo))]
        private void SetSoloOverrideColorScheme(StandardLevelScenesTransitionSetupDataSO __instance)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return;
            }

            var songData = Collections.RetrieveDifficultyData(__instance.beatmapLevel, __instance.beatmapKey);
            var overrideColorScheme = GetOverrideColorScheme(songData, __instance.colorScheme);
            if (overrideColorScheme is null)
            {
                return;
            }

            __instance.usingOverrideColorScheme = true;
            __instance.colorScheme = overrideColorScheme;
        }

        [AffinityPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.InitColorInfo))]
        private void SetMultiplayerOverrideColorScheme(MultiplayerLevelScenesTransitionSetupDataSO __instance)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return;
            }

            var songData = Collections.RetrieveDifficultyData(__instance.beatmapLevel, __instance.beatmapKey);
            var overrideColorScheme = GetOverrideColorScheme(songData, __instance.colorScheme);
            if (overrideColorScheme is null)
            {
                return;
            }

            __instance.usingOverrideColorScheme = true;
            __instance.colorScheme = overrideColorScheme;
        }

        private ColorScheme? GetOverrideColorScheme(ExtraSongData.DifficultyData? songData, ColorScheme currentColorScheme)
        {
            if (songData is null || (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null && songData._envColorRight == null &&
                                     songData._envColorWhite == null && songData._obstacleColor == null && songData._envColorLeftBoost == null && songData._envColorRightBoost == null &&
                                     songData._envColorWhiteBoost == null))
            {
                return null;
            }

            if (_config.CustomSongNoteColors)
            {
                Logging.Logger.Debug("Custom song note colors On");
            }

            if (_config.CustomSongEnvironmentColors)
            {
                Logging.Logger.Debug("Custom song environment colors On");
            }

            if (_config.CustomSongObstacleColors)
            {
                Logging.Logger.Debug("Custom song obstacle colors On");
            }

            var saberLeft = songData._colorLeft == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberAColor
                : Utils.ColorFromMapColor(songData._colorLeft);
            var saberRight = songData._colorRight == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberBColor
                : Utils.ColorFromMapColor(songData._colorRight);
            var envLeft = songData._envColorLeft == null || !_config.CustomSongEnvironmentColors
                ? songData._colorLeft == null ? currentColorScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            var envRight = songData._envColorRight == null || !_config.CustomSongEnvironmentColors
                ? songData._colorRight == null ? currentColorScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            var envWhite = songData._envColorWhite == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorW
                : Utils.ColorFromMapColor(songData._envColorWhite);
            var envLeftBoost = songData._envColorLeftBoost == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor0Boost
                : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = songData._envColorRightBoost == null|| !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor1Boost
                : Utils.ColorFromMapColor(songData._envColorRightBoost);
            var envWhiteBoost = songData._envColorWhiteBoost == null  || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorWBoost
                : Utils.ColorFromMapColor(songData._envColorWhiteBoost);
            var obstacle = songData._obstacleColor == null || !_config.CustomSongObstacleColors
                ? currentColorScheme.obstaclesColor
                : Utils.ColorFromMapColor(songData._obstacleColor);

            return new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, true, saberLeft, saberRight, true,
                envLeft, envRight, envWhite, envLeftBoost != default && envRightBoost != default, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
        }
    }
}
