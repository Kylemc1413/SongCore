using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json.Linq;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.Data
{
    [MessagePackObject]
    public class ExtraSongData
    {
        [Key(0)]
        public string[] _genreTags;

        [Key(1)]
        public Contributor[] contributors;

        [Key(2)]
        public string _customEnvironmentName;

        [Key(3)]
        public string _customEnvironmentHash;

        [Key(4)]
        public DifficultyData[] _difficulties;

        [Key(5)]
        public string _defaultCharacteristic = null;

        [Key(6)]
        public ColorScheme[] _colorSchemes;

        [Key(7)]
        public string[] _environmentNames;

        [Key(8)]
        public CharacteristicDetails[] _characteristicDetails;

        [MessagePackObject]
        public class CharacteristicDetails
        {
            [Key(0)]
            public string _beatmapCharacteristicName;

            [Key(1)]
            public string? _characteristicLabel;

            [Key(2)]
            public string? _characteristicIconFilePath = null;
        }

        [MessagePackObject]
        public class Contributor
        {
            [Key(0)]
            public string _role;

            [Key(1)]
            public string _name;

            [Key(2)]
            public string _iconPath;

            [IgnoreMember]
            public Sprite? icon = null;
        }

        [MessagePackObject]
        public class DifficultyData
        {
            [Key(0)]
            public string _beatmapCharacteristicName;

            [Key(1)]
            public BeatmapDifficulty _difficulty;

            [Key(2)]
            public string _difficultyLabel;

            [Key(3)]
            public RequirementData additionalDifficultyData;

            [Key(4)]
            public MapColor? _colorLeft;

            [Key(5)]
            public MapColor? _colorRight;

            [Key(6)]
            public MapColor? _envColorLeft;

            [Key(7)]
            public MapColor? _envColorRight;

            [Key(8)]
            public MapColor? _envColorWhite;

            [Key(9)]
            public MapColor? _envColorLeftBoost;

            [Key(10)]
            public MapColor? _envColorRightBoost;

            [Key(11)]
            public MapColor? _envColorWhiteBoost;

            [Key(12)]
            public MapColor? _obstacleColor;

            [Key(13)]
            public int? _beatmapColorSchemeIdx;

            [Key(14)]
            public int? _environmentNameIdx;

            [Key(15)]
            public bool? _oneSaber;

            [Key(16)]
            public bool? _showRotationNoteSpawnLines;

            [Key(17)]
            public string[] _styleTags;
        }

        [MessagePackObject]
        public class ColorScheme //stuck to the same naming convention as the json itself
        {
            [Key(0)]
            public bool useOverride;

            [Key(1)]
            public string colorSchemeId;

            [Key(2)]
            public MapColor? saberAColor;

            [Key(3)]
            public MapColor? saberBColor;

            [Key(4)]
            public MapColor? environmentColor0;

            [Key(5)]
            public MapColor? environmentColor1;

            [Key(6)]
            public MapColor? obstaclesColor;

            [Key(7)]
            public MapColor? environmentColor0Boost;

            [Key(8)]
            public MapColor? environmentColor1Boost;

            [Key(9)]
            public MapColor? environmentColorW;

            [Key(10)]
            public MapColor? environmentColorWBoost;
        }


        [MessagePackObject]
        public class RequirementData
        {
            [Key(0)]
            public string[] _requirements;

            [Key(1)]
            public string[] _suggestions;

            [Key(2)]
            public string[] _warnings;

            [Key(3)]
            public string[] _information;
        }

        [MessagePackObject]
        public class MapColor
        {
            [Key(0)]
            public float r;

            [Key(1)]
            public float g;

            [Key(2)]
            public float b;

            [Key(3)]
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

        internal ExtraSongData(string rawSongData, string songPath)
        {
            try
            {
                JObject info = JObject.Parse(rawSongData);
                List<Contributor> levelContributors = new List<Contributor>();
                //Check if song uses legacy value for full song One Saber mode
                if (info.TryGetValue("_customData", out var data))
                {
                    JObject infoData = (JObject) data;
                    if (infoData.TryGetValue("_contributors", out var contributors))
                    {
                        levelContributors.AddRange(contributors.ToObject<Contributor[]>());
                    }

                    if (infoData.TryGetValue("_customEnvironment", out var customEnvironment))
                    {
                        _customEnvironmentName = (string) customEnvironment;
                    }

                    if (infoData.TryGetValue("_customEnvironmentHash", out var envHash))
                    {
                        _customEnvironmentHash = (string) envHash;
                    }

                    if (infoData.TryGetValue("_defaultCharacteristic", out var defaultChar))
                    {
                        _defaultCharacteristic = (string) defaultChar;
                    }

                    var genreTags = new List<string>();
                    if (infoData.TryGetValue("_genreTags", out var genreTagsObj))
                    {
                        genreTags.AddRange(((JArray) genreTagsObj).Select(c => (string) c));
                    }
                    _genreTags = genreTags.ToArray();
                }

                contributors = levelContributors.ToArray();

                var envNames = new List<string>();
                if (info.TryGetValue("_environmentNames", out var environmentNames))
                {
                    envNames.AddRange(((JArray) environmentNames).Select(c => (string) c));
                }
                _environmentNames = envNames.ToArray();


                List<ColorScheme> colorSchemeList = new List<ColorScheme>();
                if (info.TryGetValue("_colorSchemes", out var colorSchemes)) //I DO NOT TRUST THAT PEOPLE DO THIS PROPERLY
                {
                    JArray colorSchemeListData = (JArray) colorSchemes;
                    foreach (var colorSchemeItem in colorSchemeListData)
                    {
                        JObject colorSchemeItemData = (JObject) colorSchemeItem;
                        bool _useOverride = false;
                        string _colorSchemeId = "SongCoreDefaultID";
                        MapColor? _saberAColor = null;
                        MapColor? _saberBColor = null;
                        MapColor? _environmentColor0 = null;
                        MapColor? _environmentColor1 = null;
                        MapColor? _obstaclesColor = null;
                        MapColor? _environmentColor0Boost = null;
                        MapColor? _environmentColor1Boost = null;
                        MapColor? _environmentColorW = null;
                        MapColor? _environmentColorWBoost = null;
                        if (colorSchemeItemData.TryGetValue("useOverride", out var useOverrideVal))
                        {
                            _useOverride = (bool) useOverrideVal;
                        }

                        if (colorSchemeItemData.TryGetValue("colorScheme", out var colorScheme))
                        {
                            JObject colorSchemeData = (JObject) colorScheme;

                            if (colorSchemeData.TryGetValue("colorSchemeId", out var colorSchemeIdVal))
                            {
                                _colorSchemeId = (string) colorSchemeIdVal;
                            }

                            _saberAColor = GetMapColorFromJObject(colorSchemeData, "saberAColor");
                            _saberBColor = GetMapColorFromJObject(colorSchemeData, "saberBColor");
                            _environmentColor0 = GetMapColorFromJObject(colorSchemeData, "environmentColor0");
                            _environmentColor1 = GetMapColorFromJObject(colorSchemeData, "environmentColor1");
                            _obstaclesColor = GetMapColorFromJObject(colorSchemeData, "obstaclesColor");
                            _environmentColor0Boost = GetMapColorFromJObject(colorSchemeData, "environmentColor0Boost");
                            _environmentColor1Boost = GetMapColorFromJObject(colorSchemeData, "environmentColor1Boost");
                            _environmentColorW = GetMapColorFromJObject(colorSchemeData, "environmentColorW");
                            _environmentColorWBoost = GetMapColorFromJObject(colorSchemeData, "environmentColorWBoost");

                        }

                        colorSchemeList.Add(new ColorScheme
                        {
                            useOverride = _useOverride,
                            saberAColor = _saberAColor,
                            saberBColor = _saberBColor,
                            environmentColor0 = _environmentColor0,
                            environmentColor1 = _environmentColor1,
                            obstaclesColor = _obstaclesColor,
                            environmentColor0Boost = _environmentColor0Boost,
                            environmentColor1Boost = _environmentColor1Boost,
                            environmentColorW = _environmentColorW,
                            environmentColorWBoost = _environmentColorWBoost
                        });

                    }
                }

                _colorSchemes = colorSchemeList.ToArray();


                var diffData = new List<DifficultyData>();
                var diffSets = (JArray) info["_difficultyBeatmapSets"];


                List<CharacteristicDetails> characteristicsDetails = new List<CharacteristicDetails>();

                foreach (var diffSet in diffSets)
                {
                    var setCharacteristic = (string) diffSet["_beatmapCharacteristicName"];
                    JArray diffBeatmaps = (JArray) diffSet["_difficultyBeatmaps"];
                    JObject diffBeatmapSetObj = (JObject) diffSet;


                    if (diffBeatmapSetObj.TryGetValue("_customData", out var characteristicCustom))
                    {
                        JObject characteristicCustomObj = (JObject) characteristicCustom;

                        string? characteristicLabel = null;
                        string? characteristicIconFilePath = null;

                        if (characteristicCustomObj.TryGetValue("_characteristicLabel", out var characteristicLabelObj))
                        {
                            characteristicLabel = (string) characteristicLabelObj;
                        }

                        if (characteristicCustomObj.TryGetValue("_characteristicIconImageFilename", out var characteristicIconImageFilenameObj))
                        {
                            characteristicIconFilePath = (string) characteristicIconImageFilenameObj;
                        }

                        characteristicsDetails.Add(new CharacteristicDetails
                        {
                            _characteristicLabel = characteristicLabel,
                            _beatmapCharacteristicName = setCharacteristic,
                            _characteristicIconFilePath = characteristicIconFilePath
                        });

                    }



                    foreach (JObject diffBeatmap in diffBeatmaps)
                    {
                        var diffRequirements = new List<string>();
                        var diffSuggestions = new List<string>();
                        var diffWarnings = new List<string>();
                        var diffInfo = new List<string>();
                        var diffLabel = "";
                        MapColor? diffLeft = null;
                        MapColor? diffRight = null;
                        MapColor? diffEnvLeft = null;
                        MapColor? diffEnvRight = null;
                        MapColor? diffEnvWhite = null;
                        MapColor? diffEnvLeftBoost = null;
                        MapColor? diffEnvRightBoost = null;
                        MapColor? diffEnvWhiteBoost = null;
                        MapColor? diffObstacle = null;
                        int? beatmapColorSchemeIdx = null;
                        int? environmentNameIdx = null;
                        bool? oneSaber = null;
                        bool? showRotationNoteSpawnLines = null;
                        var styleTags = new List<string>();

                        var diffDifficulty = Utils.ToEnum((string) diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);

                        if (diffBeatmap.TryGetValue("_beatmapColorSchemeIdx", out var beatmapColorSchemeIdxVal))
                        {
                            beatmapColorSchemeIdx = (int) beatmapColorSchemeIdxVal;
                        }

                        if (diffBeatmap.TryGetValue("_environmentNameIdx", out var environmentNameIdxVal))
                        {
                            environmentNameIdx = (int) environmentNameIdxVal;
                        }


                        bool useSongCoreColours = true;

                        if (beatmapColorSchemeIdx != null)
                        {
                            var colorScheme = _colorSchemes.ElementAtOrDefault(beatmapColorSchemeIdx.Value);
                            if (colorScheme != null)
                            {
                                if (colorScheme.useOverride)
                                {
                                    useSongCoreColours = false;
                                    diffLeft = colorScheme.saberAColor;
                                    diffRight = colorScheme.saberBColor;
                                    diffEnvLeft = colorScheme.environmentColor0;
                                    diffEnvRight = colorScheme.environmentColor1;
                                    diffEnvWhite = colorScheme.environmentColorW;
                                    diffEnvLeftBoost = colorScheme.environmentColor0Boost;
                                    diffEnvRightBoost = colorScheme.environmentColor1Boost;
                                    diffEnvWhiteBoost = colorScheme.environmentColorWBoost;
                                    diffObstacle = colorScheme.obstaclesColor;
                                }
                            }
                        }

                        if (diffBeatmap.TryGetValue("_customData", out var customData))
                        {
                            JObject beatmapData = (JObject) customData;
                            if (info.TryGetValue("_styleTags", out var tagObj))
                            {
                                styleTags.AddRange(((JArray) tagObj).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_difficultyLabel", out var difficultyLabel))
                            {
                                diffLabel = (string) difficultyLabel;
                            }

                            if (useSongCoreColours)
                            {
                                //Get difficulty json fields

                                diffLeft = GetMapColorFromJObject(beatmapData, "_colorLeft");
                                diffRight = GetMapColorFromJObject(beatmapData, "_colorRight");
                                diffEnvLeft = GetMapColorFromJObject(beatmapData, "_envColorLeft");
                                diffEnvRight = GetMapColorFromJObject(beatmapData, "_envColorRight");
                                diffEnvWhite = GetMapColorFromJObject(beatmapData, "_envColorWhite");
                                diffEnvLeftBoost = GetMapColorFromJObject(beatmapData, "_envColorLeftBoost");
                                diffEnvRightBoost = GetMapColorFromJObject(beatmapData, "_envColorRightBoost");
                                diffEnvWhiteBoost = GetMapColorFromJObject(beatmapData, "_envColorWhiteBoost");
                                diffObstacle = GetMapColorFromJObject(beatmapData, "_obstacleColor");

                            }

                            if (beatmapData.TryGetValue("_warnings", out var warnings))
                            {
                                diffWarnings.AddRange(((JArray) warnings).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_information", out var information))
                            {
                                diffInfo.AddRange(((JArray) information).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_suggestions", out var suggestions))
                            {
                                diffSuggestions.AddRange(((JArray) suggestions).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_requirements", out var requirements))
                            {
                                diffRequirements.AddRange(((JArray) requirements).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_oneSaber", out var oneSaberObj))
                            {
                                oneSaber = (bool) oneSaberObj;
                            }

                            if (beatmapData.TryGetValue("_showRotationNoteSpawnLines", out var showRotationNoteSpawnLinesObj))
                            {
                                showRotationNoteSpawnLines = (bool) showRotationNoteSpawnLinesObj;
                            }

                        }

                        RequirementData diffReqData = new RequirementData
                        {
                            _requirements = diffRequirements.ToArray(),
                            _suggestions = diffSuggestions.ToArray(),
                            _information = diffInfo.ToArray(),
                            _warnings = diffWarnings.ToArray()
                        };

                        diffData.Add(new DifficultyData
                        {
                            _beatmapCharacteristicName = setCharacteristic,
                            _difficulty = diffDifficulty,
                            _difficultyLabel = diffLabel,
                            additionalDifficultyData = diffReqData,
                            _colorLeft = diffLeft,
                            _colorRight = diffRight,
                            _envColorLeft = diffEnvLeft,
                            _envColorRight = diffEnvRight,
                            _envColorWhite = diffEnvWhite,
                            _envColorLeftBoost = diffEnvLeftBoost,
                            _envColorRightBoost = diffEnvRightBoost,
                            _envColorWhiteBoost = diffEnvWhiteBoost,
                            _obstacleColor = diffObstacle,
                            _beatmapColorSchemeIdx = beatmapColorSchemeIdx,
                            _environmentNameIdx = environmentNameIdx,
                            _oneSaber = oneSaber,
                            _showRotationNoteSpawnLines = showRotationNoteSpawnLines,
                            _styleTags = styleTags.ToArray()
                        });
                    }
                }

                _difficulties = diffData.ToArray();
                _characteristicDetails = characteristicsDetails.ToArray();
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error in Level {songPath}:");
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