using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            this._customEnvironmentName = customEnvironmentName;
            this._customEnvironmentHash = customEnvironmentHash;
            this._difficulties = difficulties;

        }

        public ExtraSongData(string levelID, string songPath)
        {
            //        Utilities.Logging.Log("SongData Ctor");
            try
            {
                if (!File.Exists(songPath + "/info.dat")) return;
                var infoText = File.ReadAllText(songPath + "/info.dat");

                JObject info = JObject.Parse(infoText);
                JObject infoData;
                List<Contributor> levelContributors = new List<Contributor>();
                //Check if song uses legacy value for full song One Saber mode
                if (info.ContainsKey("_customData"))
                {
                    infoData = (JObject)info["_customData"];
                    if (infoData.ContainsKey("_contributors"))
                    {
                        levelContributors.AddRange(infoData["_contributors"].ToObject<Contributor[]>());
                    }
                    if (infoData.ContainsKey("_customEnvironment")) _customEnvironmentName = (string)infoData["_customEnvironment"];
                    if (infoData.ContainsKey("_customEnvironmentHash")) _customEnvironmentHash = (string)infoData["_customEnvironmentHash"];

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
                        BeatmapDifficulty diffDifficulty = Utilities.Utils.ToEnum((string)diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);
                        JObject beatmapData;
                        if(diffBeatmap.ContainsKey("_customData"))
                        {
                            beatmapData = (JObject)diffBeatmap["_customData"];
                            if (beatmapData.ContainsKey("_difficultyLabel")) diffLabel = (string)beatmapData["_difficultyLabel"];

                            //Get difficulty json fields
                            if (beatmapData.ContainsKey("_colorLeft"))
                            {
                                if (beatmapData["_colorLeft"].Children().Count() == 3)
                                {
                                    diffLeft = new MapColor(0, 0, 0);
                                    diffLeft.r = (float)beatmapData["_colorLeft"]["r"];
                                    diffLeft.g = (float)beatmapData["_colorLeft"]["g"];
                                    diffLeft.b = (float)beatmapData["_colorLeft"]["b"];
                                }


                            }
                            if (beatmapData.ContainsKey("_colorRight"))
                            {
                                if (beatmapData["_colorRight"].Children().Count() == 3)
                                {
                                    diffRight = new MapColor(0, 0, 0);
                                    diffRight.r = (float)beatmapData["_colorRight"]["r"];
                                    diffRight.g = (float)beatmapData["_colorRight"]["g"];
                                    diffRight.b = (float)beatmapData["_colorRight"]["b"];
                                }

                            }
                            if (beatmapData.ContainsKey("_warnings"))
                                diffWarnings.AddRange(((JArray)beatmapData["_warnings"]).Select(c => (string)c));
                            if (beatmapData.ContainsKey("_information"))
                                diffInfo.AddRange(((JArray)beatmapData["_information"]).Select(c => (string)c));
                            if (beatmapData.ContainsKey("_suggestions"))
                                diffSuggestions.AddRange(((JArray)beatmapData["_suggestions"]).Select(c => (string)c));
                            if (beatmapData.ContainsKey("_requirements"))
                                diffRequirements.AddRange(((JArray)beatmapData["_requirements"]).Select(c => (string)c));
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
                            _colorRight = diffRight

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


