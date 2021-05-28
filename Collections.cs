using Newtonsoft.Json;
using SongCore.Data;
using SongCore.Utilities;
using IPA.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SongCore
{
    public static class Collections
    {
        internal static CustomBeatmapLevelPack WipLevelPack;


        internal static string dataPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "UserData", "SongCore", "SongCoreExtraData.dat");
        internal static ConcurrentDictionary<string, ExtraSongData> customSongsData = new ConcurrentDictionary<string, ExtraSongData>();
        internal static ConcurrentDictionary<string, string> levelHashDictionary = new ConcurrentDictionary<string, string>();
        internal static ConcurrentDictionary<string, List<string>> hashLevelDictionary = new ConcurrentDictionary<string, List<string>>();
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
            if (hashLevelDictionary.ContainsKey(hash.ToUpper()))
                return true;
            else
                return false;
        }
        public static string hashForLevelID(string levelID)
        {
            if (levelHashDictionary.TryGetValue(levelID, out var hash))
                return hash;
            return "";
        }
        public static List<string> levelIDsForHash(string hash)
        {
            if (hashLevelDictionary.TryGetValue(hash.ToUpper(), out var songs))
                return songs;
            return new List<string>();
        }

        public static void AddSong(string levelID, string path)
        {
            AddExtraSongData(levelID, new ExtraSongData(levelID, path));
            //         Utilities.Logging.Log("Entry: :"  + levelID + "    " + customSongsData.Count);
        }

        public static void AddExtraSongData(string levelID, ExtraSongData extraSongData)
        {
            if (!customSongsData.ContainsKey(levelID))
                customSongsData.TryAdd(levelID, extraSongData);
        }

        public static ExtraSongData RetrieveExtraSongData(string levelID, string loadIfNullPath = "")
        {
            //      Logging.Log(levelID);
            //      Logging.Log(loadIfNullPath);
            if (customSongsData.TryGetValue(levelID, out var songData))
                return songData;

            if (!string.IsNullOrWhiteSpace(loadIfNullPath))
            {
                AddSong(levelID, loadIfNullPath);

                if (customSongsData.TryGetValue(levelID, out songData))
                    return songData;
            }

            return null;
        }

        public static ExtraSongData.DifficultyData RetrieveDifficultyData(IDifficultyBeatmap beatmap)
        {
            ExtraSongData songData = null;
            if (beatmap.level is CustomPreviewBeatmapLevel)
            {
                var customLevel = beatmap.level as CustomPreviewBeatmapLevel;
                songData = RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
            }
            if (songData == null) return null;
            ExtraSongData.DifficultyData diffData = songData._difficulties.FirstOrDefault(x => x._difficulty == beatmap.difficulty && (x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicNameLocalizationKey || x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName));
            return diffData;
        }
        public static void LoadExtraSongData()
        {
            customSongsData = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, ExtraSongData>>(File.ReadAllText(dataPath));
            if (customSongsData == null)
                customSongsData = new ConcurrentDictionary<string, ExtraSongData>();
        }

        public static void SaveExtraSongData()
        {
            File.WriteAllText(dataPath, Newtonsoft.Json.JsonConvert.SerializeObject(customSongsData, Formatting.None));
        }

        public static void RegisterCapability(string capability)
        {
            if (!_capabilities.Contains(capability))
                _capabilities.Add(capability);
        }

        public static BeatmapCharacteristicSO RegisterCustomCharacteristic(Sprite Icon, string CharacteristicName, string HintText, string SerializedName, string CompoundIdPartName, bool requires360Movement = false, bool containsRotationEvents = false, int sortingOrder = 99)
        {
            BeatmapCharacteristicSO newChar = ScriptableObject.CreateInstance<BeatmapCharacteristicSO>();

            newChar.SetField("_icon", Icon);
            newChar.SetField("_descriptionLocalizationKey", HintText);
            newChar.SetField("_serializedName", SerializedName);
            newChar.SetField("_characteristicNameLocalizationKey", CharacteristicName);
            newChar.SetField("_compoundIdPartName", CompoundIdPartName);
            newChar.SetField("_requires360Movement", requires360Movement);
            newChar.SetField("_containsRotationEvents", containsRotationEvents);
            newChar.SetField("_sortingOrder", sortingOrder);
            if (!_customCharacteristics.Any(x => x.serializedName == newChar.serializedName))
            {
                _customCharacteristics.Add(newChar);
                return newChar;
            }

            return null;
        }
        //SongFolderEntry(string name, string path, FolderLevelPack pack, string imagePath = "", bool wip = false)
        public static SeperateSongFolder AddSeperateSongFolder(string name, string folderPath, FolderLevelPack pack, Sprite image = null, bool wip = false, bool cachezips = false)
        {
            UI.BasicUI.GetIcons();
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    Logging.logger.Error("Failed to make folder for: " + name + "\n" + ex);
                }
            }
            Data.SongFolderEntry entry = new SongFolderEntry(name, folderPath, pack, "", wip, cachezips);
            ModSeperateSongFolder seperateSongFolder = new ModSeperateSongFolder(entry, image == null ? UI.BasicUI.FolderIcon : image);
            if (Loader.SeperateSongFolders == null) Loader.SeperateSongFolders = new List<SeperateSongFolder>();
            Loader.SeperateSongFolders.Add(seperateSongFolder);
            return seperateSongFolder;
        }


        public static void DeregisterizeCapability(string capability)
        {
            _capabilities.Remove(capability);
        }


    }
}
