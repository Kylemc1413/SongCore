using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongCore.Data;
using Newtonsoft.Json;
using UnityEngine;
using SongCore.Utilities;
namespace SongCore
{
    public static class Collections
    {
        internal static CustomBeatmapLevelPack WipLevelPack;


        internal static string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongCoreExtraData.dat");
        internal static Dictionary<string, ExtraSongData> customSongsData = new Dictionary<string, ExtraSongData>();
        internal static Dictionary<string, string> levelHashDictionary = new Dictionary<string, string>();
        internal static Dictionary<string, List<string>> hashLevelDictionary = new Dictionary<string, List<string>>();
        private static List<string> _capabilities = new List<string>();
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> capabilities
        {
            get { return _capabilities.AsReadOnly(); }
        }

        private static List<BeatmapCharacteristicSO> _customCharacteristics = new List<BeatmapCharacteristicSO>();
        public static System.Collections.ObjectModel.ReadOnlyCollection<BeatmapCharacteristicSO> customCharacteristics
        {
            get { return _customCharacteristics.AsReadOnly(); }
        }


        public static bool songWithHashPresent(string hash)
        {
            if (hashLevelDictionary.ContainsKey(hash))
                return true;
            else
                return false;
        }
        public static string hashForLevelID(string levelID)
        {
            levelHashDictionary.TryGetValue(levelID, out var hash);
            return hash ?? "";
        }
        public static List<string> levelIDsForHash(string hash)
        {
            if (hashLevelDictionary.TryGetValue(hash, out var songs))
                return songs;
            songs = new List<string>();
            songs.Add(string.Empty);
            return songs;
        }
        
        public static void AddSong(string levelID, string path)
        {

            if (!customSongsData.ContainsKey(levelID))
                customSongsData.Add(levelID, new ExtraSongData(levelID, path));
            //         Utilities.Logging.Log("Entry: :"  + levelID + "    " + customSongsData.Count);
        }

        public static ExtraSongData RetrieveExtraSongData(string levelID, string loadIfNullPath = "")
        {
      //      Logging.Log(levelID);
      //      Logging.Log(loadIfNullPath);
            if (customSongsData.ContainsKey(levelID))
                return customSongsData[levelID];

            if (!string.IsNullOrWhiteSpace(loadIfNullPath))
            {
                AddSong(levelID, loadIfNullPath);

                if (customSongsData.ContainsKey(levelID))
                    return customSongsData[levelID];
            }

            return null;
        }

        public static ExtraSongData.DifficultyData RetrieveDifficultyData(IDifficultyBeatmap beatmap)
        {
            ExtraSongData songData = null;
            if (beatmap.level is CustomPreviewBeatmapLevel)
            {
                var customLevel = beatmap.level as CustomPreviewBeatmapLevel;
                songData = RetrieveExtraSongData(Utils.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
            }
            if (songData == null) return null;
            ExtraSongData.DifficultyData diffData = songData._difficulties.FirstOrDefault(x => x._difficulty == beatmap.difficulty && (x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicName || x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName));
            return diffData;
        }
        public static void LoadExtraSongData()
        {
            customSongsData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ExtraSongData>>(File.ReadAllText(dataPath));
            if (customSongsData == null)
                customSongsData = new Dictionary<string, ExtraSongData>();
        }

        public static void SaveExtraSongData()
        {
            File.WriteAllText(dataPath, Newtonsoft.Json.JsonConvert.SerializeObject(customSongsData));
        }



        public static void RegisterCapability(string capability)
        {
            if (!_capabilities.Contains(capability))
                _capabilities.Add(capability);
        }

        public static BeatmapCharacteristicSO RegisterCustomCharacteristic(Sprite Icon, string CharacteristicName, string HintText, string SerializedName, string CompoundIdPartName)
        {
            BeatmapCharacteristicSO newChar = ScriptableObject.CreateInstance<BeatmapCharacteristicSO>();

            newChar.SetField("_icon", Icon);
            newChar.SetField("_hintText", HintText);
            newChar.SetField("_serializedName", SerializedName);
            newChar.SetField("_characteristicName", CharacteristicName);
            newChar.SetField("_compoundIdPartName", CompoundIdPartName);

            if (!_customCharacteristics.Any(x => x.serializedName == newChar.serializedName))
            {
                _customCharacteristics.Add(newChar);
                return newChar;
            }

            return null;
        }



        public static void DeregisterizeCapability(string capability)
        {
            _capabilities.Remove(capability);
        }


    }
}
