using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;
namespace SongCore.Data
{


    [Serializable]
    public class ExtraSongData
    {
        public string songPath;
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string customEnvironmentName;
        public string customEnvironmentHash;
        public DifficultyData[] difficulties;

        [Serializable]
        public class Contributor
        {
            public string role;
            public string name;
            public string iconPath;
            [NonSerialized]
            public Sprite icon = null;

        }
        [Serializable]
        public class DifficultyData
        {
            public string beatmapCharacteristicName;
            public BeatmapDifficulty difficulty;
            public string difficultyLabel;
            public RequirementData additionalDifficultyData;
            public MapColor colorLeft;
            public MapColor colorRight;
        }
        [Serializable]
        public class RequirementData
        {
            public string[] requirements;
            public string[] suggestions;
            public string[] warnings;
            public string[] information;
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
        public ExtraSongData(string levelID, string songPath, Contributor[] contributors, string customEnvironmentName, string customEnvironmentHash, DifficultyData[] difficulties)
        {
      //      Utilities.Logging.Log("SongData full Ctor");
            this.songPath = songPath;
            this.contributors = contributors;
            this.customEnvironmentName = customEnvironmentName;
            this.customEnvironmentHash = customEnvironmentHash;
            this.difficulties = difficulties;

        }
        public ExtraSongData(string levelID, string songPath)
        {
    //        Utilities.Logging.Log("SongData Ctor");
            try
            {
                this.songPath = songPath;
                if (!File.Exists(songPath + "/info.json")) return;
                var infoText = File.ReadAllText(songPath + "/info.json");

                JObject info = JObject.Parse(infoText);
                //Check if song uses legacy value for full song One Saber mode
                bool legacyOneSaber = false;
                if (info.ContainsKey("oneSaber")) legacyOneSaber = (bool)info["oneSaber"];

                List<Contributor> levelContributors = new List<Contributor>();
                if (info.ContainsKey("contributors"))
                {
                    levelContributors.AddRange(info["contributors"].ToObject<Contributor[]>());
                }
                if(info.ContainsKey("mappers"))
                {
                    foreach (JToken mapper in (JArray)info["mappers"])
                        levelContributors.Add(new Contributor
                        {
                            name = (string)mapper,
                            role = "Mapper",
                            iconPath = ""
                        });
                }
                if (info.ContainsKey("lighters"))
                {
                    foreach (JToken lighter in (JArray)info["lighters"])
                        levelContributors.Add(new Contributor
                        {
                            name = (string)lighter,
                            role = "Lighter",
                            iconPath = ""
                        });
                }
                contributors = levelContributors.ToArray();
                if (info.ContainsKey("customEnvironment")) customEnvironmentName = (string)info["customEnvironment"];
                if (info.ContainsKey("customEnvironmentHash")) customEnvironmentHash = (string)info["customEnvironmentHash"];
                List<DifficultyData> diffData = new List<DifficultyData>();
                JArray diffLevels = (JArray)info["difficultyLevels"];
                foreach (JObject diff in diffLevels)
                {
                    //       Utilities.Logging.Log((string)diff["difficulty"]);
                    if (!File.Exists(songPath + "/" + diff["jsonPath"])) continue;
                    string diffText = File.ReadAllText(songPath + "/" + diff["jsonPath"]);

                    List<string> diffRequirements = new List<string>();
                    List<string> diffSuggestions = new List<string>();
                    List<string> diffWarnings = new List<string>();
                    List<string> diffInfo = new List<string>();
                    var split = diffText.Split(':');
                    JObject diffFile = JObject.Parse(diffText);
                    try
                    {
                        for (var i = 0; i < split.Length; i++)
                        {
                            int value;
                            if (split[i].Contains("_lineIndex"))
                            {
                                value = Convert.ToInt32(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                                if ((value < 0 || value > 3) && !(value >= 1000 || value <= -1000))
                                    if (!diffRequirements.Contains("Mapping Extensions-More Lanes")) diffRequirements.Add("Mapping Extensions-More Lanes");
                                if (value >= 1000 || value <= -1000)
                                    if (!diffRequirements.Contains("Mapping Extensions-Precision Placement")) diffRequirements.Add("Mapping Extensions-Precision Placement");
                            }
                            if (split[i].Contains("_lineLayer"))
                            {
                                value = Convert.ToInt32(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                                if ((value < 0 || value > 2) && !(value >= 1000 || value <= -1000))
                                    if (!diffRequirements.Contains("Mapping Extensions-More Lanes")) diffRequirements.Add("Mapping Extensions-More Lanes");
                                if (value >= 1000 || value <= -1000)
                                    if (!diffRequirements.Contains("Mapping Extensions-Precision Placement")) diffRequirements.Add("Mapping Extensions-Precision Placement");
                            }
                            if (split[i].Contains("_cutDirection"))
                            {
                                value = Convert.ToInt32(split[i + 1].Split(',', '}')[0], CultureInfo.InvariantCulture);
                                if ((value >= 1000 && value <= 1360) || (value >= 2000 && value <= 2360))
                                    if (!diffRequirements.Contains("Mapping Extensions-Extra Note Angles")) diffRequirements.Add("Mapping Extensions-Extra Note Angles");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Logging.Log($"Exception in Parsing Split JSON of: {songPath}", IPA.Logging.Logger.Level.Warning);
                    }

                    string diffCharacteristic = legacyOneSaber ? Plugin.oneSaberCharacteristicName : Plugin.standardCharacteristicName;
                    if (diff.ContainsKey("characteristic")) diffCharacteristic = (string)diff["characteristic"];
                    switch (diffCharacteristic)
                    {
                        case "Standard":
                            diffCharacteristic = Plugin.standardCharacteristicName;
                            break;
                        case "One Saber":
                            diffCharacteristic = Plugin.oneSaberCharacteristicName;
                            break;
                        case "No Arrows":
                            diffCharacteristic = Plugin.noArrowsCharacteristicName;
                            break;
                    }


                    BeatmapDifficulty diffDifficulty = Utilities.Utils.ToEnum((string)diff["difficulty"], BeatmapDifficulty.Normal);

                    string diffLabel = "";
                    if (diff.ContainsKey("difficultyLabel")) diffLabel = (string)diff["difficultyLabel"];


                    //Get difficulty json fields
                    MapColor diffLeft = null;
                    if (diffFile.ContainsKey("_colorLeft"))
                    {
                        diffLeft = new MapColor(0, 0, 0);
                        diffLeft.r = (float)diffFile["_colorLeft"]["r"];
                        diffLeft.g = (float)diffFile["_colorLeft"]["g"];
                        diffLeft.b = (float)diffFile["_colorLeft"]["b"];
                    }
                    MapColor diffRight = null;
                    if (diffFile.ContainsKey("_colorRight"))
                    {
                        diffRight = new MapColor(0, 0, 0);
                        diffRight.r = (float)diffFile["_colorRight"]["r"];
                        diffRight.g = (float)diffFile["_colorRight"]["g"];
                        diffRight.b = (float)diffFile["_colorRight"]["b"];
                    }

                    if (diffFile.ContainsKey("_requirements"))
                        diffRequirements.AddRange(((JArray)diffFile["_requirements"]).Select(c => (string)c));
                    if (diffFile.ContainsKey("_suggestions"))
                        diffSuggestions.AddRange(((JArray)diffFile["_suggestions"]).Select(c => (string)c));
                    if (diffFile.ContainsKey("_warnings"))
                        diffWarnings.AddRange(((JArray)diffFile["_warnings"]).Select(c => (string)c));
                    if (diffFile.ContainsKey("_information"))
                        diffInfo.AddRange(((JArray)diffFile["_information"]).Select(c => (string)c));
                    RequirementData diffReqData = new RequirementData
                    {
                        requirements = diffRequirements.ToArray(),
                        suggestions = diffSuggestions.ToArray(),
                        information = diffInfo.ToArray(),
                        warnings = diffWarnings.ToArray()
                    };

                    diffData.Add(new DifficultyData
                    {
                        beatmapCharacteristicName = diffCharacteristic,
                        difficulty = diffDifficulty,
                        difficultyLabel = diffLabel,
                        additionalDifficultyData = diffReqData,
                        colorLeft = diffLeft,
                        colorRight = diffRight

                    }
                    );

                }
                difficulties = diffData.ToArray();

            }
            catch (Exception ex)
            {
                Utilities.Logging.Log($"Error in Level {songPath}: \n {ex}", IPA.Logging.Logger.Level.Error);
            }
        }

        public void UpdateData(string songPath)
        {
            this.songPath = songPath;
            if (!File.Exists(songPath + "/info.json")) return;
            var infoText = File.ReadAllText(songPath + "/info.json");
            try
            {
                JObject info = JObject.Parse(infoText);
                //Check if song uses legacy value for full song One Saber mode
                bool legacyOneSaber = false;
                if (info.ContainsKey("oneSaber")) legacyOneSaber = (bool)info["oneSaber"];


                if (info.ContainsKey("contributors"))
                {
                    contributors = info["contributors"].ToObject<Contributor[]>();
                }
                else
                {
                    contributors = new Contributor[0];
                }
                if (info.ContainsKey("customEnvironment")) customEnvironmentName = (string)info["customEnvironment"];
                if (info.ContainsKey("customEnvironmentHash")) customEnvironmentHash = (string)info["customEnvironmentHash"];
                List<DifficultyData> diffData = difficulties?.ToList();
                if (diffData == null) return;
                JArray diffLevels = (JArray)info["difficultyLevels"];
                for (int i = 0; i < diffData.Count; ++i)
                {
                    var json = (JObject)diffLevels[i];

                    diffData[i].difficulty = Utilities.Utils.ToEnum((string)json["difficulty"], BeatmapDifficulty.Normal);
                    diffData[i].beatmapCharacteristicName = json.ContainsKey("characteristic") ? (string)json["characteristic"] : legacyOneSaber ? Plugin.oneSaberCharacteristicName : Plugin.standardCharacteristicName;
                    switch (diffData[i].beatmapCharacteristicName)
                    {
                        case "Standard":
                            diffData[i].beatmapCharacteristicName = Plugin.standardCharacteristicName;
                            break;
                        case "One Saber":
                            diffData[i].beatmapCharacteristicName = Plugin.oneSaberCharacteristicName;
                            break;
                        case "No Arrows":
                            diffData[i].beatmapCharacteristicName = Plugin.noArrowsCharacteristicName;
                            break;
                    }
                    diffData[i].difficultyLabel = "";
                    if (json.ContainsKey("difficultyLabel")) diffData[i].difficultyLabel = (string)json["difficultyLabel"];
                }

                difficulties = diffData.ToArray();
            }
            catch (Exception ex)
            {
                Utilities.Logging.Log($"Error in Level {songPath}: \n {ex}", IPA.Logging.Logger.Level.Error);
            }
        }




 
    }
}


