using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            public Sprite icon = null;

        }
        [Serializable]
        public class DifficultyData
        {
            public string _beatmapCharacteristicName;
            public BeatmapDifficulty _difficulty;
            public string _difficultyLabel;
            public RequirementData additionalDifficultyData;
            public MapColor _colorLeft;
            public MapColor _colorRight;
            public MapColor _envColorLeft;
            public MapColor _envColorRight;
            public MapColor _envColorLeftBoost;
            public MapColor _envColorRightBoost;
            public MapColor _obstacleColor;
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

        [Newtonsoft.Json.JsonConstructor]
        public ExtraSongData(string levelID, Contributor[] contributors, string customEnvironmentName, string customEnvironmentHash, DifficultyData[] difficulties)
        {
            //      Utilities.Logging.Log("SongData full Ctor");
            this.contributors = contributors;
            _customEnvironmentName = customEnvironmentName;
            _customEnvironmentHash = customEnvironmentHash;
            _difficulties = difficulties;

        }

        public ExtraSongData(string levelID, string songPath)
        {
            //        Utilities.Logging.Log("SongData Ctor");
            try
            {
                if (!File.Exists(songPath + "/info.dat"))
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
                    infoData = (JObject)data;
                    if (infoData.TryGetValue("_contributors", out var contributors))
                    {
                        levelContributors.AddRange(contributors.ToObject<Contributor[]>());
                    }
                    if (infoData.TryGetValue("_customEnvironment", out var customEnvironment))
                    {
                        _customEnvironmentName = (string)customEnvironment;
                    }

                    if (infoData.TryGetValue("_customEnvironmentHash", out var envHash))
                    {
                        _customEnvironmentHash = (string)envHash;
                    }

                    if (infoData.TryGetValue("_defaultCharacteristic", out var defaultChar))
                    {
                        _defaultCharacteristic = (string)defaultChar;
                    }
                }
                contributors = levelContributors.ToArray();


                List<DifficultyData> diffData = new List<DifficultyData>();
                JArray diffSets = (JArray)info["_difficultyBeatmapSets"];
                foreach (JObject diffSet in diffSets)
                {
                    string SetCharacteristic = (string)diffSet["_beatmapCharacteristicName"];
                    JArray diffBeatmaps = (JArray)diffSet["_difficultyBeatmaps"];
                    foreach (JObject diffBeatmap in diffBeatmaps)
                    {
                        List<string> diffRequirements = new List<string>();
                        List<string> diffSuggestions = new List<string>();
                        List<string> diffWarnings = new List<string>();
                        List<string> diffInfo = new List<string>();
                        string diffLabel = "";
                        MapColor diffLeft = null;
                        MapColor diffRight = null;
                        MapColor diffEnvLeft = null;
                        MapColor diffEnvRight = null;
                        MapColor diffEnvLeftBoost = null;
                        MapColor diffEnvRightBoost = null;
                        MapColor diffObstacle = null;

                        var diffDifficulty = Utilities.Utils.ToEnum((string)diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);
                        JObject beatmapData;
                        if (diffBeatmap.TryGetValue("_customData", out var customData))
                        {
                            beatmapData = (JObject)customData;
                            if (beatmapData.TryGetValue("_difficultyLabel", out var difficultyLabel))
                            {
                                diffLabel = (string)difficultyLabel;
                            }

                            //Get difficulty json fields
                            if (beatmapData.TryGetValue("_colorLeft", out var colorLeft))
                            {
                                if (colorLeft.Children().Count() == 3)
                                {
                                    diffLeft = new MapColor(0, 0, 0);
                                    diffLeft.r = (float)beatmapData["_colorLeft"]["r"];
                                    diffLeft.g = (float)beatmapData["_colorLeft"]["g"];
                                    diffLeft.b = (float)beatmapData["_colorLeft"]["b"];
                                }


                            }
                            if (beatmapData.TryGetValue("_colorRight", out var colorRight))
                            {
                                if (colorRight.Children().Count() == 3)
                                {
                                    diffRight = new MapColor(0, 0, 0);
                                    diffRight.r = (float)beatmapData["_colorRight"]["r"];
                                    diffRight.g = (float)beatmapData["_colorRight"]["g"];
                                    diffRight.b = (float)beatmapData["_colorRight"]["b"];
                                }

                            }

                            if (beatmapData.TryGetValue("_envColorLeft", out var envColorLeft))
                            {
                                if (envColorLeft.Children().Count() == 3)
                                {
                                    diffEnvLeft = new MapColor(0, 0, 0);
                                    diffEnvLeft.r = (float)beatmapData["_envColorLeft"]["r"];
                                    diffEnvLeft.g = (float)beatmapData["_envColorLeft"]["g"];
                                    diffEnvLeft.b = (float)beatmapData["_envColorLeft"]["b"];
                                }

                            }

                            if (beatmapData.TryGetValue("_envColorRight", out var envColorRight))
                            {
                                if (envColorRight.Children().Count() == 3)
                                {
                                    diffEnvRight = new MapColor(0, 0, 0);
                                    diffEnvRight.r = (float)beatmapData["_envColorRight"]["r"];
                                    diffEnvRight.g = (float)beatmapData["_envColorRight"]["g"];
                                    diffEnvRight.b = (float)beatmapData["_envColorRight"]["b"];
                                }

                            }
                            if (beatmapData.TryGetValue("_envColorLeftBoost", out var envColorLeftBoost))
                            {
                                if (envColorLeftBoost.Children().Count() == 3)
                                {
                                    diffEnvLeftBoost = new MapColor(0, 0, 0);
                                    diffEnvLeftBoost.r = (float)beatmapData["_envColorLeftBoost"]["r"];
                                    diffEnvLeftBoost.g = (float)beatmapData["_envColorLeftBoost"]["g"];
                                    diffEnvLeftBoost.b = (float)beatmapData["_envColorLeftBoost"]["b"];
                                }

                            }

                            if (beatmapData.TryGetValue("_envColorRightBoost", out var envColorRightBoost))
                            {
                                if (envColorRightBoost.Children().Count() == 3)
                                {
                                    diffEnvRightBoost = new MapColor(0, 0, 0);
                                    diffEnvRightBoost.r = (float)beatmapData["_envColorRightBoost"]["r"];
                                    diffEnvRightBoost.g = (float)beatmapData["_envColorRightBoost"]["g"];
                                    diffEnvRightBoost.b = (float)beatmapData["_envColorRightBoost"]["b"];
                                }

                            }
                            if (beatmapData.TryGetValue("_obstacleColor", out var obColor))
                            {
                                if (obColor.Children().Count() == 3)
                                {
                                    diffObstacle = new MapColor(0, 0, 0);
                                    diffObstacle.r = (float)beatmapData["_obstacleColor"]["r"];
                                    diffObstacle.g = (float)beatmapData["_obstacleColor"]["g"];
                                    diffObstacle.b = (float)beatmapData["_obstacleColor"]["b"];
                                }

                            }

                            if (beatmapData.TryGetValue("_warnings", out var warnings))
                            {
                                diffWarnings.AddRange(((JArray)warnings).Select(c => (string)c));
                            }

                            if (beatmapData.TryGetValue("_information", out var information))
                            {
                                diffInfo.AddRange(((JArray)information).Select(c => (string)c));
                            }

                            if (beatmapData.TryGetValue("_suggestions", out var suggestions))
                            {
                                diffSuggestions.AddRange(((JArray)suggestions).Select(c => (string)c));
                            }

                            if (beatmapData.TryGetValue("_requirements", out var requirements))
                            {
                                diffRequirements.AddRange(((JArray)requirements).Select(c => (string)c));
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
                            _beatmapCharacteristicName = SetCharacteristic,
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
                Utilities.Logging.Log($"Error in Level {songPath}: \n {ex}", IPA.Logging.Logger.Level.Error);
            }
        }
    }
}


