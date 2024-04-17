using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.Data
{
    [Serializable]
    public class ExtraSongData
    {
        public string[] _genreTags;
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string _customEnvironmentName;
        public string _customEnvironmentHash;
        public DifficultyData[] _difficulties;
        public string _defaultCharacteristic = null;

        public ColorScheme[] _colorSchemes; //beatmap 2.1.0, community decided to song-core ify colour stuff
        public string[] _environmentNames; //these have underscores but the actual format doesnt, I genuinely dont know what to go by so I went consistent with songcore

        //PinkCore Port
        public CharacteristicDetails[] _characteristicDetails;

        [Serializable]
        public class CharacteristicDetails
        {
            public string _beatmapCharacteristicName;
            public string? _characteristicLabel;
            public string? _characteristicIconFilePath = null;
        }


        [Serializable]
        public class Contributor
        {
            public string _role;
            public string _name;
            public string _iconPath;

            [NonSerialized]
            public Sprite? icon = null;
        }

        [Serializable]
        public class DifficultyData
        {
            public string _beatmapCharacteristicName;
            public BeatmapDifficulty _difficulty;
            public string _difficultyLabel;
            public RequirementData additionalDifficultyData;
            public MapColor? _colorLeft;
            public MapColor? _colorRight;
            public MapColor? _envColorLeft;
            public MapColor? _envColorRight;
            public MapColor? _envColorWhite;
            public MapColor? _envColorLeftBoost;
            public MapColor? _envColorRightBoost;
            public MapColor? _envColorWhiteBoost;
            public MapColor? _obstacleColor;
            public int? _beatmapColorSchemeIdx;
            public int? _environmentNameIdx;

            //PinkCore Port
            public bool? _oneSaber;
            public bool? _showRotationNoteSpawnLines;
            //Tags
            public string[] _styleTags;
        }

        [Serializable]
        public class ColorScheme //stuck to the same naming convention as the json itself
        {
            public bool useOverride;
            public string colorSchemeId;
            public MapColor? saberAColor;
            public MapColor? saberBColor;
            public MapColor? environmentColor0;
            public MapColor? environmentColor1;
            public MapColor? obstaclesColor;
            public MapColor? environmentColor0Boost;
            public MapColor? environmentColor1Boost;
            //Not officially within the default scheme, added for consistency
            public MapColor? environmentColorW;
            public MapColor? environmentColorWBoost;
        }


        [Serializable]
        public class RequirementData
        {
            public string[] _requirements;
            public string[] _suggestions;
            public string[] _warnings;
            public string[] _information;
        }

        [Serializable]
        public class MapColor
        {
            public float r;
            public float g;
            public float b;

            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public float a = 1f;

            public MapColor(float r, float g, float b, float a = 1f)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }
        }

        public ExtraSongData()
        {
        }

        [JsonConstructor]
        public ExtraSongData(string levelID, Contributor[] contributors, string customEnvironmentName, string customEnvironmentHash, DifficultyData[] difficulties)
        {
            this.contributors = contributors;
            _customEnvironmentName = customEnvironmentName;
            _customEnvironmentHash = customEnvironmentHash;
            _difficulties = difficulties;
        }

        internal ExtraSongData(CustomLevelLoader.LoadedSaveData loadedSaveData)
        {
            try
            {
                var levelInfo = JObject.Parse(loadedSaveData.customLevelFolderInfo.levelInfoJsonString);
                if (loadedSaveData.standardLevelInfoSaveData != null)
                {
                    var difficultyData = new List<DifficultyData>();
                    var characteristicsDetails = new List<CharacteristicDetails>();

                    if (levelInfo.TryGetValue("_customData", out var customDataToken))
                    {
                        var customData = (JObject)customDataToken;
                        contributors = customData.TryGetValue("_contributors", out var contributorsToken)
                            ? contributorsToken.ToObject<Contributor[]>()
                            : Array.Empty<Contributor>();
                        _genreTags = customData.TryGetValue("_genreTags", out var genreTagsToken)
                            ? genreTagsToken.ToObject<string[]>()
                            : Array.Empty<string>();
                        _customEnvironmentName = customData.Value<string>("_customEnvironment");
                        _customEnvironmentHash = customData.Value<string>("_customEnvironmentHash");
                        _defaultCharacteristic = customData.Value<string>("_defaultCharacteristic");
                    }
                    else
                    {
                        contributors = Array.Empty<Contributor>();
                    }

                    _environmentNames = levelInfo.TryGetValue("_environmentNames", out var environmentNamesToken)
                        ? environmentNamesToken.ToObject<string[]>()
                        : Array.Empty<string>();

                    if (levelInfo.TryGetValue("_colorSchemes", out var colorSchemesToken))
                    {
                        _colorSchemes = colorSchemesToken.Select(c =>
                        {
                            var colorSchemeSaveData = (JObject)c;
                            if (!colorSchemeSaveData.TryGetValue("colorScheme", out var colorSchemeToken))
                            {
                                return null;
                            }

                            var colorScheme = (JObject)colorSchemeToken;
                            var useOverride = colorSchemeSaveData.Value<bool>("useOverride");
                            return new ColorScheme
                            {
                                useOverride = useOverride,
                                // colorSchemeId = colorScheme.Value<string>("colorSchemeId") ?? "SongCoreDefaultID",
                                saberAColor = GetMapColorFromJObject(colorScheme, "saberAColor"),
                                saberBColor = GetMapColorFromJObject(colorScheme, "saberBColor"),
                                environmentColor0 = GetMapColorFromJObject(colorScheme, "environmentColor0"),
                                environmentColor1 = GetMapColorFromJObject(colorScheme, "environmentColor1"),
                                obstaclesColor = GetMapColorFromJObject(colorScheme, "obstaclesColor"),
                                environmentColor0Boost = GetMapColorFromJObject(colorScheme, "environmentColor0Boost"),
                                environmentColor1Boost = GetMapColorFromJObject(colorScheme, "environmentColor1Boost"),
                                environmentColorW = GetMapColorFromJObject(colorScheme, "environmentColorW"),
                                environmentColorWBoost = GetMapColorFromJObject(colorScheme, "environmentColorWBoost")
                            };
                        }).Where(c => c is not null).ToArray()!;
                    }
                    else
                    {
                        _colorSchemes = Array.Empty<ColorScheme>();
                    }

                    var difficultyBeatmapSets = (JArray)levelInfo["_difficultyBeatmapSets"];
                    foreach (var difficultyBeatmapSetToken in difficultyBeatmapSets)
                    {
                        var beatmapCharacteristicName = (string)difficultyBeatmapSetToken["_beatmapCharacteristicName"];
                        var difficultyBeatmapSet = (JObject)difficultyBeatmapSetToken;
                        var difficultyBeatmaps = (JArray)difficultyBeatmapSetToken["_difficultyBeatmaps"];

                        if (difficultyBeatmapSet.TryGetValue("_customData", out var customCharacteristicDataToken))
                        {
                            var customData = (JObject)customCharacteristicDataToken;
                            characteristicsDetails.Add(new CharacteristicDetails
                            {
                                _beatmapCharacteristicName = beatmapCharacteristicName,
                                _characteristicLabel = customData.Value<string>("_characteristicLabel"),
                                _characteristicIconFilePath = customData.Value<string>("_characteristicIconImageFilename")
                            });
                        }

                        foreach (var difficultyBeatmapToken in difficultyBeatmaps)
                        {
                            var difficultyBeatmap = (JObject)difficultyBeatmapToken;

                            string[]? requirements = null;
                            string[]? suggestions = null;
                            string[]? warnings = null;
                            string[]? information = null;
                            string? difficultyLabel = null;
                            MapColor? colorLeft = null;
                            MapColor? colorRight = null;
                            MapColor? envColorLeft = null;
                            MapColor? envColorRight = null;
                            MapColor? envColorWhite = null;
                            MapColor? envColorLeftBoost = null;
                            MapColor? envColorRightBoost = null;
                            MapColor? envColorWhiteBoost = null;
                            MapColor? obstacleColor = null;
                            bool? oneSaber = null;
                            bool? showRotationNoteSpawnLines = null;
                            string[]? styleTags = null;

                            var difficulty = Utils.ToEnum((string)difficultyBeatmap["_difficulty"], BeatmapDifficulty.Normal);
                            var beatmapColorSchemeIdx = difficultyBeatmap.Value<int?>("_beatmapColorSchemeIdx");
                            var environmentNameIdx = difficultyBeatmap.Value<int?>("_environmentNameIdx");
                            bool useSongCoreColors = true;

                            if (beatmapColorSchemeIdx != null)
                            {
                                var colorScheme = _colorSchemes.ElementAtOrDefault(beatmapColorSchemeIdx.Value);
                                if (colorScheme is { useOverride: true })
                                {
                                    useSongCoreColors = false;
                                    colorLeft = colorScheme.saberAColor;
                                    colorRight = colorScheme.saberBColor;
                                    envColorLeft = colorScheme.environmentColor0;
                                    envColorRight = colorScheme.environmentColor1;
                                    envColorWhite = colorScheme.environmentColorW;
                                    envColorLeftBoost = colorScheme.environmentColor0Boost;
                                    envColorRightBoost = colorScheme.environmentColor1Boost;
                                    envColorWhiteBoost = colorScheme.environmentColorWBoost;
                                    obstacleColor = colorScheme.obstaclesColor;
                                }
                            }

                            if (difficultyBeatmap.TryGetValue("_customData", out var customDifficultyDataToken))
                            {
                                var customData = (JObject)customDifficultyDataToken;

                                styleTags = levelInfo.TryGetValue("_styleTags", out var styleTagsToken)
                                    ? styleTagsToken.ToObject<string[]>()
                                    : Array.Empty<string>();
                                requirements = customData.TryGetValue("_requirements", out var requirementsToken)
                                    ? requirementsToken.ToObject<string[]>()
                                    : Array.Empty<string>();
                                suggestions = customData.TryGetValue("_suggestions", out var suggestionsToken)
                                    ? suggestionsToken.ToObject<string[]>()
                                    : Array.Empty<string>();
                                warnings = customData.TryGetValue("_warnings", out var warningsToken)
                                    ? warningsToken.ToObject<string[]>()
                                    : Array.Empty<string>();
                                information = customData.TryGetValue("_information", out var informationToken)
                                    ? informationToken.ToObject<string[]>()
                                    : Array.Empty<string>();
                                difficultyLabel = customData.Value<string>("_difficultyLabel");
                                oneSaber = customData.Value<bool?>("_oneSaber");
                                showRotationNoteSpawnLines = customData.Value<bool?>("_showRotationNoteSpawnLines");

                                if (useSongCoreColors)
                                {
                                    colorLeft = GetMapColorFromJObject(customData, "_colorLeft");
                                    colorRight = GetMapColorFromJObject(customData, "_colorRight");
                                    envColorLeft = GetMapColorFromJObject(customData, "_envColorLeft");
                                    envColorRight = GetMapColorFromJObject(customData, "_envColorRight");
                                    envColorWhite = GetMapColorFromJObject(customData, "_envColorWhite");
                                    envColorLeftBoost = GetMapColorFromJObject(customData, "_envColorLeftBoost");
                                    envColorRightBoost = GetMapColorFromJObject(customData, "_envColorRightBoost");
                                    envColorWhiteBoost = GetMapColorFromJObject(customData, "_envColorWhiteBoost");
                                    obstacleColor = GetMapColorFromJObject(customData, "_obstacleColor");
                                }
                            }

                            var requirementData = new RequirementData
                            {
                                _requirements = requirements ?? Array.Empty<string>(),
                                _suggestions = suggestions ?? Array.Empty<string>(),
                                _information = information ?? Array.Empty<string>(),
                                _warnings = warnings ?? Array.Empty<string>()
                            };

                            difficultyData.Add(new DifficultyData
                            {
                                _beatmapCharacteristicName = beatmapCharacteristicName,
                                _difficulty = difficulty,
                                _difficultyLabel = difficultyLabel ?? string.Empty,
                                additionalDifficultyData = requirementData,
                                _colorLeft = colorLeft,
                                _colorRight = colorRight,
                                _envColorLeft = envColorLeft,
                                _envColorRight = envColorRight,
                                _envColorWhite = envColorWhite,
                                _envColorLeftBoost = envColorLeftBoost,
                                _envColorRightBoost = envColorRightBoost,
                                _envColorWhiteBoost = envColorWhiteBoost,
                                _obstacleColor = obstacleColor,
                                _beatmapColorSchemeIdx = beatmapColorSchemeIdx,
                                _environmentNameIdx = environmentNameIdx,
                                _oneSaber = oneSaber,
                                _showRotationNoteSpawnLines = showRotationNoteSpawnLines,
                                _styleTags = styleTags ?? Array.Empty<string>()
                            });
                        }
                    }

                    _difficulties = difficultyData.ToArray();
                    _characteristicDetails = characteristicsDetails.ToArray();
                }
                else if (loadedSaveData.beatmapLevelSaveData != null)
                {
                    var difficultyData = new List<DifficultyData>();

                    if (levelInfo.TryGetValue("customData", out var customDataToken))
                    {
                        var customData = (JObject)customDataToken;
                        contributors = customData.TryGetValue("contributors", out var contributorsToken)
                            ? contributorsToken
                                .Select(contributor => new Contributor
                                {
                                    _role = contributor.Value<string>("role"),
                                    _name = contributor.Value<string>("name"),
                                    _iconPath = contributor.Value<string>("iconPath")
                                })
                                .ToArray()
                            : Array.Empty<Contributor>();
                        _genreTags = customData.TryGetValue("genreTags", out var genreTagsToken)
                            ? genreTagsToken.ToObject<string[]>()
                            : Array.Empty<string>();
                        _customEnvironmentName = customData.Value<string>("customEnvironment");
                        _customEnvironmentHash = customData.Value<string>("customEnvironmentHash");
                        _defaultCharacteristic = customData.Value<string>("defaultCharacteristic");

                        if (customData.TryGetValue("characteristicData", out var characteristicDataToken))
                        {
                            var characteristicData = (JArray)characteristicDataToken;
                            _characteristicDetails = characteristicData
                                    .Select(characteristic => new CharacteristicDetails
                                    {
                                        _beatmapCharacteristicName = characteristic.Value<string>("characteristic"),
                                        _characteristicLabel = characteristic.Value<string>("label"),
                                        _characteristicIconFilePath = characteristic.Value<string>("iconPath")
                                    })
                                .ToArray();
                        }
                    }
                    else
                    {
                        contributors = Array.Empty<Contributor>();
                        _characteristicDetails = Array.Empty<CharacteristicDetails>();
                    }

                    _environmentNames = levelInfo.TryGetValue("environmentNames", out var environmentNamesToken)
                        ? environmentNamesToken.ToObject<string[]>()
                        : Array.Empty<string>();

                    if (levelInfo.TryGetValue("colorSchemes", out var colorSchemesToken))
                    {
                        _colorSchemes = colorSchemesToken.Select(c =>
                        {
                            var colorSchemeSaveData = (JObject)c;
                            if (!colorSchemeSaveData.TryGetValue("colorScheme", out var colorSchemeToken))
                            {
                                return null;
                            }

                            var colorScheme = (JObject)colorSchemeToken;
                            var useOverride = colorSchemeSaveData.Value<bool>("useOverride");
                            return new ColorScheme
                            {
                                useOverride = useOverride,
                                // colorSchemeId = colorScheme.Value<string>("colorSchemeId") ?? "SongCoreDefaultID",
                                saberAColor = GetMapColorFromJObject(colorScheme, "saberAColor"),
                                saberBColor = GetMapColorFromJObject(colorScheme, "saberBColor"),
                                environmentColor0 = GetMapColorFromJObject(colorScheme, "environmentColor0"),
                                environmentColor1 = GetMapColorFromJObject(colorScheme, "environmentColor1"),
                                obstaclesColor = GetMapColorFromJObject(colorScheme, "obstaclesColor"),
                                environmentColor0Boost = GetMapColorFromJObject(colorScheme, "environmentColor0Boost"),
                                environmentColor1Boost = GetMapColorFromJObject(colorScheme, "environmentColor1Boost"),
                                environmentColorW = GetMapColorFromJObject(colorScheme, "environmentColorW"),
                                environmentColorWBoost = GetMapColorFromJObject(colorScheme, "environmentColorWBoost")
                            };
                        }).Where(c => c is not null).ToArray()!;
                    }
                    else
                    {
                        _colorSchemes = Array.Empty<ColorScheme>();
                    }

                    var difficultyBeatmaps = (JArray)levelInfo["difficultyBeatmaps"];
                    foreach (var difficultyBeatmapToken in difficultyBeatmaps)
                    {
                        var difficultyBeatmap = (JObject)difficultyBeatmapToken;
                        var beatmapCharacteristicName = (string)difficultyBeatmap["characteristic"];

                        string[]? requirements = null;
                        string[]? suggestions = null;
                        string[]? warnings = null;
                        string[]? information = null;
                        string? difficultyLabel = null;
                        MapColor? colorLeft = null;
                        MapColor? colorRight = null;
                        MapColor? envColorLeft = null;
                        MapColor? envColorRight = null;
                        MapColor? envColorWhite = null;
                        MapColor? envColorLeftBoost = null;
                        MapColor? envColorRightBoost = null;
                        MapColor? envColorWhiteBoost = null;
                        MapColor? obstacleColor = null;
                        bool? oneSaber = null;
                        bool? showRotationNoteSpawnLines = null;
                        string[]? styleTags = null;

                        var difficulty = Utils.ToEnum((string)difficultyBeatmap["difficulty"], BeatmapDifficulty.Normal);
                        var beatmapColorSchemeIdx = difficultyBeatmap.Value<int?>("beatmapColorSchemeIdx");
                        var environmentNameIdx = difficultyBeatmap.Value<int?>("environmentNameIdx");
                        bool useSongCoreColors = true;

                        if (beatmapColorSchemeIdx != null)
                        {
                            var colorScheme = _colorSchemes.ElementAtOrDefault(beatmapColorSchemeIdx.Value);
                            if (colorScheme is { useOverride: true })
                            {
                                useSongCoreColors = false;
                                colorLeft = colorScheme.saberAColor;
                                colorRight = colorScheme.saberBColor;
                                envColorLeft = colorScheme.environmentColor0;
                                envColorRight = colorScheme.environmentColor1;
                                envColorWhite = colorScheme.environmentColorW;
                                envColorLeftBoost = colorScheme.environmentColor0Boost;
                                envColorRightBoost = colorScheme.environmentColor1Boost;
                                envColorWhiteBoost = colorScheme.environmentColorWBoost;
                                obstacleColor = colorScheme.obstaclesColor;
                            }
                        }

                        if (difficultyBeatmap.TryGetValue("customData", out var customDifficultyDataToken))
                        {
                            var customData = (JObject)customDifficultyDataToken;

                            styleTags = levelInfo.TryGetValue("styleTags", out var styleTagsToken)
                                ? styleTagsToken.ToObject<string[]>()
                                : Array.Empty<string>();
                            requirements = customData.TryGetValue("requirements", out var requirementsToken)
                                ? requirementsToken.ToObject<string[]>()
                                : Array.Empty<string>();
                            suggestions = customData.TryGetValue("suggestions", out var suggestionsToken)
                                ? suggestionsToken.ToObject<string[]>()
                                : Array.Empty<string>();
                            warnings = customData.TryGetValue("warnings", out var warningsToken)
                                ? warningsToken.ToObject<string[]>()
                                : Array.Empty<string>();
                            information = customData.TryGetValue("information", out var informationToken)
                                ? informationToken.ToObject<string[]>()
                                : Array.Empty<string>();
                            difficultyLabel = customData.Value<string>("difficultyLabel");
                            oneSaber = customData.Value<bool?>("oneSaber");
                            showRotationNoteSpawnLines = customData.Value<bool?>("showRotationNoteSpawnLines");

                            if (useSongCoreColors)
                            {
                                colorLeft = GetMapColorFromJObject(customData, "colorLeft");
                                colorRight = GetMapColorFromJObject(customData, "colorRight");
                                envColorLeft = GetMapColorFromJObject(customData, "envColorLeft");
                                envColorRight = GetMapColorFromJObject(customData, "envColorRight");
                                envColorWhite = GetMapColorFromJObject(customData, "envColorWhite");
                                envColorLeftBoost = GetMapColorFromJObject(customData, "envColorLeftBoost");
                                envColorRightBoost = GetMapColorFromJObject(customData, "envColorRightBoost");
                                envColorWhiteBoost = GetMapColorFromJObject(customData, "envColorWhiteBoost");
                                obstacleColor = GetMapColorFromJObject(customData, "obstacleColor");
                            }
                        }

                        var requirementData = new RequirementData
                        {
                            _requirements = requirements ?? Array.Empty<string>(),
                            _suggestions = suggestions ?? Array.Empty<string>(),
                            _information = information ?? Array.Empty<string>(),
                            _warnings = warnings ?? Array.Empty<string>()
                        };

                        difficultyData.Add(new DifficultyData
                        {
                            _beatmapCharacteristicName = beatmapCharacteristicName,
                            _difficulty = difficulty,
                            _difficultyLabel = difficultyLabel ?? string.Empty,
                            additionalDifficultyData = requirementData,
                            _colorLeft = colorLeft,
                            _colorRight = colorRight,
                            _envColorLeft = envColorLeft,
                            _envColorRight = envColorRight,
                            _envColorWhite = envColorWhite,
                            _envColorLeftBoost = envColorLeftBoost,
                            _envColorRightBoost = envColorRightBoost,
                            _envColorWhiteBoost = envColorWhiteBoost,
                            _obstacleColor = obstacleColor,
                            _beatmapColorSchemeIdx = beatmapColorSchemeIdx,
                            _environmentNameIdx = environmentNameIdx,
                            _oneSaber = oneSaber,
                            _showRotationNoteSpawnLines = showRotationNoteSpawnLines,
                            _styleTags = styleTags ?? Array.Empty<string>()
                        });
                    }

                    _difficulties = difficultyData.ToArray();
                }
                else
                {
                    throw new InvalidOperationException("Level save data is missing.");
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error in Level {loadedSaveData.customLevelFolderInfo.folderPath}:");
                Logging.Logger.Error(ex);
            }
        }

        public static MapColor? GetMapColorFromJObject(JObject jObject, string key)
        {
            if (jObject.TryGetValue(key, out var envColorWhiteBoost))
            {
                if (envColorWhiteBoost.Children().Count() >= 3)
                {
                    return new MapColor(
                        (float) (envColorWhiteBoost["r"] ?? 0),
                        (float) (envColorWhiteBoost["g"] ?? 0),
                        (float) (envColorWhiteBoost["b"] ?? 0),
                        (float) (envColorWhiteBoost["a"] ?? 1));
                }
            }
            return null;
        }
    }
}