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
                if (!File.Exists(songPath + "/info.dat")) return;
                var infoText = File.ReadAllText(songPath + "/info.dat");

                JObject info = JObject.Parse(infoText);
                //Check if song uses legacy value for full song One Saber mode

                List<Contributor> levelContributors = new List<Contributor>();
                if (info.ContainsKey("contributors"))
                {
                    levelContributors.AddRange(info["contributors"].ToObject<Contributor[]>());
                }

                contributors = levelContributors.ToArray();
                if (info.ContainsKey("customEnvironment")) customEnvironmentName = (string)info["customEnvironment"];
                if (info.ContainsKey("customEnvironmentHash")) customEnvironmentHash = (string)info["customEnvironmentHash"];
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

                        BeatmapDifficulty diffDifficulty = Utilities.Utils.ToEnum((string)diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);

                        string diffLabel = "";
                        if (diffBeatmap.ContainsKey("difficultyLabel")) diffLabel = (string)diffBeatmap["difficultyLabel"];


                        //Get difficulty json fields
                        MapColor diffLeft = null;
                        if (diffBeatmap.ContainsKey("_colorLeft"))
                        {
                            diffLeft = new MapColor(0, 0, 0);
                            diffLeft.r = (float)diffBeatmap["_colorLeft"]["r"];
                            diffLeft.g = (float)diffBeatmap["_colorLeft"]["g"];
                            diffLeft.b = (float)diffBeatmap["_colorLeft"]["b"];
                        }
                        MapColor diffRight = null;
                        if (diffBeatmap.ContainsKey("_colorRight"))
                        {
                            diffRight = new MapColor(0, 0, 0);
                            diffRight.r = (float)diffBeatmap["_colorRight"]["r"];
                            diffRight.g = (float)diffBeatmap["_colorRight"]["g"];
                            diffRight.b = (float)diffBeatmap["_colorRight"]["b"];
                        }

                        if (diffBeatmap.ContainsKey("_requirements"))
                            diffRequirements.AddRange(((JArray)diffBeatmap["_requirements"]).Select(c => (string)c));
                        if (diffBeatmap.ContainsKey("_suggestions"))
                            diffSuggestions.AddRange(((JArray)diffBeatmap["_suggestions"]).Select(c => (string)c));
                        if (diffBeatmap.ContainsKey("_warnings"))
                            diffWarnings.AddRange(((JArray)diffBeatmap["_warnings"]).Select(c => (string)c));
                        if (diffBeatmap.ContainsKey("_information"))
                            diffInfo.AddRange(((JArray)diffBeatmap["_information"]).Select(c => (string)c));


                        if (!File.Exists(songPath + "/" + diffBeatmap["_beatmapFilename"])) continue;
                        string diffText = File.ReadAllText(songPath + "/" + diffBeatmap["_beatmapFilename"]);
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

                        RequirementData diffReqData = new RequirementData
                        {
                            requirements = diffRequirements.ToArray(),
                            suggestions = diffSuggestions.ToArray(),
                            information = diffInfo.ToArray(),
                            warnings = diffWarnings.ToArray()
                        };

                        diffData.Add(new DifficultyData
                        {
                            beatmapCharacteristicName = SetCharacteristic,
                            difficulty = diffDifficulty,
                            difficultyLabel = diffLabel,
                            additionalDifficultyData = diffReqData,
                            colorLeft = diffLeft,
                            colorRight = diffRight

                        });
                    }
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


