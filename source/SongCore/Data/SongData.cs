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
    public class SongData
    {
        public string RawSongData;
        public StandardLevelInfoSaveData SaveData;

        public SongData(string rawSongData, StandardLevelInfoSaveData saveData)
        {
            RawSongData = rawSongData;
            SaveData = saveData;
        }
    }

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