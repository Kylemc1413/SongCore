using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.Data
{
    [Serializable]
    public class ExtraSongData
    {
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string _customEnvironmentName;
        public string _customEnvironmentHash;
        public DifficultyData[] _difficulties;
        public string _defaultCharacteristic = null;

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
            public MapColor? _envColorLeftBoost;
            public MapColor? _envColorRightBoost;
            public MapColor? _obstacleColor;
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


            public MapColor(float r, float g, float b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
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

        public ExtraSongData(string levelID, string songPath)
        {
            try
            {
                if (!File.Exists(Path.Combine(songPath, "info.dat")))
                {
                    return;
                }

                var infoText = File.ReadAllText(songPath + "/info.dat");

                JObject info = JObject.Parse(infoText);
                JObject infoData;
                List<Contributor> levelContributors = new List<Contributor>();
                //Check if song uses legacy value for full song One Saber mode
                if (info.TryGetValue("_customData", out var data))
                {
                    infoData = (JObject) data;
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
                }

                contributors = levelContributors.ToArray();


                var diffData = new List<DifficultyData>();
                var diffSets = (JArray) info["_difficultyBeatmapSets"];
                foreach (var diffSet in diffSets)
                {
                    var setCharacteristic = (string) diffSet["_beatmapCharacteristicName"];
                    JArray diffBeatmaps = (JArray) diffSet["_difficultyBeatmaps"];
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
                        MapColor? diffEnvLeftBoost = null;
                        MapColor? diffEnvRightBoost = null;
                        MapColor? diffObstacle = null;

                        var diffDifficulty = Utilities.Utils.ToEnum((string) diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);
                        if (diffBeatmap.TryGetValue("_customData", out var customData))
                        {
                            JObject beatmapData = (JObject) customData;
                            if (beatmapData.TryGetValue("_difficultyLabel", out var difficultyLabel))
                            {
                                diffLabel = (string) difficultyLabel;
                            }

                            //Get difficulty json fields
                            if (beatmapData.TryGetValue("_colorLeft", out var colorLeft))
                            {
                                if (colorLeft.Children().Count() == 3)
                                {
                                    diffLeft = new MapColor(
                                        (float) (colorLeft["r"] ?? 0),
                                        (float) (colorLeft["g"] ?? 0),
                                        (float) (colorLeft["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_colorRight", out var colorRight))
                            {
                                if (colorRight.Children().Count() == 3)
                                {
                                    diffRight = new MapColor(
                                        (float) (colorRight["r"] ?? 0),
                                        (float) (colorRight["g"] ?? 0),
                                        (float) (colorRight["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_envColorLeft", out var envColorLeft))
                            {
                                if (envColorLeft.Children().Count() == 3)
                                {
                                    diffEnvLeft = new MapColor(
                                        (float) (envColorLeft["r"] ?? 0),
                                        (float) (envColorLeft["g"] ?? 0),
                                        (float) (envColorLeft["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_envColorRight", out var envColorRight))
                            {
                                if (envColorRight.Children().Count() == 3)
                                {
                                    diffEnvRight = new MapColor(
                                        (float) (envColorRight["r"] ?? 0),
                                        (float) (envColorRight["g"] ?? 0),
                                        (float) (envColorRight["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_envColorLeftBoost", out var envColorLeftBoost))
                            {
                                if (envColorLeftBoost.Children().Count() == 3)
                                {
                                    diffEnvLeftBoost = new MapColor(
                                        (float) (envColorLeftBoost["r"] ?? 0),
                                        (float) (envColorLeftBoost["g"] ?? 0),
                                        (float) (envColorLeftBoost["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_envColorRightBoost", out var envColorRightBoost))
                            {
                                if (envColorRightBoost.Children().Count() == 3)
                                {
                                    diffEnvRightBoost = new MapColor(
                                        (float) (envColorRightBoost["r"] ?? 0),
                                        (float) (envColorRightBoost["g"] ?? 0),
                                        (float) (envColorRightBoost["b"] ?? 0));
                                }
                            }

                            if (beatmapData.TryGetValue("_obstacleColor", out var obColor))
                            {
                                if (obColor.Children().Count() == 3)
                                {
                                    diffObstacle = new MapColor(
                                        (float) (obColor["r"] ?? 0),
                                        (float) (obColor["g"] ?? 0),
                                        (float) (obColor["b"] ?? 0));
                                }
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
                            _envColorLeftBoost = diffEnvLeftBoost,
                            _envColorRightBoost = diffEnvRightBoost,
                            _obstacleColor = diffObstacle
                        });
                    }
                }

                _difficulties = diffData.ToArray();
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error in Level {songPath}:");
                Logging.Logger.Error(ex);
            }
        }
    }
}