using System;
using System.Linq;
using SongCore.Data;

namespace SongCore.Utilities
{
    internal static class SongDataExtensions
    {
        [Obsolete]
        public static ExtraSongData ToExtraSongData(this SongData songData)
        {
            var contributors = songData.contributors?.Select(c => new ExtraSongData.Contributor
            {
                _role = c._role,
                _name = c._name,
                _iconPath = c._iconPath,
                icon = c.icon
            }).ToArray();

            var difficulties = songData._difficulties?.Select(d => new ExtraSongData.DifficultyData
            {
                _beatmapCharacteristicName = d._beatmapCharacteristicName,
                _difficulty = d._difficulty,
                _difficultyLabel = d._difficultyLabel,
                additionalDifficultyData = d.additionalDifficultyData != null ? new ExtraSongData.RequirementData
                {
                    _requirements = d.additionalDifficultyData._requirements,
                    _suggestions = d.additionalDifficultyData._suggestions,
                    _warnings = d.additionalDifficultyData._warnings,
                    _information = d.additionalDifficultyData._information
                } : null,
                _colorLeft = d._colorLeft != null ? new ExtraSongData.MapColor(d._colorLeft.r, d._colorLeft.g, d._colorLeft.b, d._colorLeft.a) : null,
                _colorRight = d._colorRight != null ? new ExtraSongData.MapColor(d._colorRight.r, d._colorRight.g, d._colorRight.b, d._colorRight.a) : null,
                _envColorLeft = d._envColorLeft != null ? new ExtraSongData.MapColor(d._envColorLeft.r, d._envColorLeft.g, d._envColorLeft.b, d._envColorLeft.a) : null,
                _envColorRight = d._envColorRight != null ? new ExtraSongData.MapColor(d._envColorRight.r, d._envColorRight.g, d._envColorRight.b, d._envColorRight.a) : null,
                _envColorWhite = d._envColorWhite != null ? new ExtraSongData.MapColor(d._envColorWhite.r, d._envColorWhite.g, d._envColorWhite.b, d._envColorWhite.a) : null,
                _envColorLeftBoost = d._envColorLeftBoost != null ? new ExtraSongData.MapColor(d._envColorLeftBoost.r, d._envColorLeftBoost.g, d._envColorLeftBoost.b, d._envColorLeftBoost.a) : null,
                _envColorRightBoost = d._envColorRightBoost != null ? new ExtraSongData.MapColor(d._envColorRightBoost.r, d._envColorRightBoost.g, d._envColorRightBoost.b, d._envColorRightBoost.a) : null,
                _envColorWhiteBoost = d._envColorWhiteBoost != null ? new ExtraSongData.MapColor(d._envColorWhiteBoost.r, d._envColorWhiteBoost.g, d._envColorWhiteBoost.b, d._envColorWhiteBoost.a) : null,
                _obstacleColor = d._obstacleColor != null ? new ExtraSongData.MapColor(d._obstacleColor.r, d._obstacleColor.g, d._obstacleColor.b, d._obstacleColor.a) : null,
                _beatmapColorSchemeIdx = d._beatmapColorSchemeIdx,
                _environmentNameIdx = d._environmentNameIdx,
                _oneSaber = d._oneSaber,
                _showRotationNoteSpawnLines = d._showRotationNoteSpawnLines,
                _styleTags = d._styleTags
            }).ToArray();

            var colorSchemes = songData._colorSchemes?.Select(c => new ExtraSongData.ColorScheme
            {
                useOverride = c.useOverride,
                colorSchemeId = c.colorSchemeId,
                saberAColor = c.saberAColor != null ? new ExtraSongData.MapColor(c.saberAColor.r, c.saberAColor.g, c.saberAColor.b, c.saberAColor.a) : null,
                saberBColor = c.saberBColor != null ? new ExtraSongData.MapColor(c.saberBColor.r, c.saberBColor.g, c.saberBColor.b, c.saberBColor.a) : null,
                environmentColor0 = c.environmentColor0 != null ? new ExtraSongData.MapColor(c.environmentColor0.r, c.environmentColor0.g, c.environmentColor0.b, c.environmentColor0.a) : null,
                environmentColor1 = c.environmentColor1 != null ? new ExtraSongData.MapColor(c.environmentColor1.r, c.environmentColor1.g, c.environmentColor1.b, c.environmentColor1.a) : null,
                obstaclesColor = c.obstaclesColor != null ? new ExtraSongData.MapColor(c.obstaclesColor.r, c.obstaclesColor.g, c.obstaclesColor.b, c.obstaclesColor.a) : null,
                environmentColor0Boost = c.environmentColor0Boost != null ? new ExtraSongData.MapColor(c.environmentColor0Boost.r, c.environmentColor0Boost.g, c.environmentColor0Boost.b, c.environmentColor0Boost.a) : null,
                environmentColor1Boost = c.environmentColor1Boost != null ? new ExtraSongData.MapColor(c.environmentColor1Boost.r, c.environmentColor1Boost.g, c.environmentColor1Boost.b, c.environmentColor1Boost.a) : null,
                environmentColorW = c.environmentColorW != null ? new ExtraSongData.MapColor(c.environmentColorW.r, c.environmentColorW.g, c.environmentColorW.b, c.environmentColorW.a) : null,
                environmentColorWBoost = c.environmentColorWBoost != null ? new ExtraSongData.MapColor(c.environmentColorWBoost.r, c.environmentColorWBoost.g, c.environmentColorWBoost.b, c.environmentColorWBoost.a) : null
            }).ToArray();

            var characteristicDetails = songData._characteristicDetails?.Select(detail => new ExtraSongData.CharacteristicDetails
            {
                _beatmapCharacteristicName = detail._beatmapCharacteristicName,
                _characteristicLabel = detail._characteristicLabel,
                _characteristicIconFilePath = detail._characteristicIconFilePath
            }).ToArray();

            return new ExtraSongData
            {
                _genreTags = songData._genreTags,
                contributors = contributors,
                _customEnvironmentName = songData._customEnvironmentName,
                _customEnvironmentHash = songData._customEnvironmentHash,
                _difficulties = difficulties,
                _defaultCharacteristic = songData._defaultCharacteristic,
                _colorSchemes = colorSchemes,
                _environmentNames = songData._environmentNames,
                _characteristicDetails = characteristicDetails
            };
        }
    }
}
