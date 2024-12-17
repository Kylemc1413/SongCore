using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SongCore.Data
{
    [Obsolete("Use SongData instead.", true)]
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
