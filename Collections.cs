using Newtonsoft.Json;
using SongCore.Data;
using SongCore.Utilities;
using IPA.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SongCore
{
    public static class Collections
    {
        private static readonly List<string> _capabilities = new List<string>();
        private static readonly List<BeatmapCharacteristicSO> _customCharacteristics = new List<BeatmapCharacteristicSO>();

        internal static readonly string DataPath = Path.Combine(UnityGame.UserDataPath, "SongCore", "SongCoreExtraData.dat");
        internal static readonly ConcurrentDictionary<string, string> LevelHashDictionary = new ConcurrentDictionary<string, string>();
        internal static readonly ConcurrentDictionary<string, List<string>> HashLevelDictionary = new ConcurrentDictionary<string, List<string>>();

        internal static CustomBeatmapLevelPack? WipLevelPack;
        internal static ConcurrentDictionary<string, ExtraSongData> CustomSongsData = new ConcurrentDictionary<string, ExtraSongData>();

        public static ReadOnlyCollection<string> capabilities => _capabilities.AsReadOnly();
        public static ReadOnlyCollection<BeatmapCharacteristicSO> customCharacteristics => _customCharacteristics.AsReadOnly();

        public static bool songWithHashPresent(string hash)
        {
            return HashLevelDictionary.ContainsKey(hash.ToUpper());
        }

        public static string hashForLevelID(string levelID)
        {
            return LevelHashDictionary.TryGetValue(levelID, out var hash) ? hash : string.Empty;
        }

        public static List<string> levelIDsForHash(string hash)
        {
            return HashLevelDictionary.TryGetValue(hash.ToUpper(), out var songs) ? songs : new List<string>();
        }

        internal static void AddExtraSongData(string hash, string path, string rawSongData)
        {
            if (!CustomSongsData.ContainsKey(hash))
            {
                CustomSongsData.TryAdd(hash, new ExtraSongData(rawSongData, path));
            }
        }

        public static ExtraSongData? RetrieveExtraSongData(string hash)
        {
            if (CustomSongsData.TryGetValue(hash, out var songData))
            {
                return songData;
            }

            return null;
        }

        public static ExtraSongData.DifficultyData? RetrieveDifficultyData(IDifficultyBeatmap beatmap)
        {
            ExtraSongData? songData = null;

            if (beatmap.level is CustomPreviewBeatmapLevel customLevel)
            {
                songData = RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel));
            }

            var diffData = songData?._difficulties.FirstOrDefault(x =>
                x._difficulty == beatmap.difficulty && (x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicNameLocalizationKey ||
                                                        x._beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName));

            return diffData;
        }

        internal static void LoadExtraSongData()
        {
            Task.Run(() =>
            {
                try
                {
                    using var reader = new JsonTextReader(new StreamReader(DataPath));
                    var serializer = JsonSerializer.CreateDefault();
                    CustomSongsData = serializer.Deserialize<ConcurrentDictionary<string, ExtraSongData>?>(reader) ?? new ConcurrentDictionary<string, ExtraSongData>();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error($"Error loading extra song data: {ex.Message}");
                    Logging.Logger.Debug(ex);
                }
            });
        }

        internal static async Task SaveExtraSongDataAsync()
        {
            using var writer = new StreamWriter(DataPath);
            await writer.WriteAsync(JsonConvert.SerializeObject(CustomSongsData, Formatting.None));
        }

        public static void RegisterCapability(string capability)
        {
            if (!_capabilities.Contains(capability))
            {
                _capabilities.Add(capability);
            }
        }

        public static BeatmapCharacteristicSO? RegisterCustomCharacteristic(Sprite icon, string characteristicName, string hintText, string serializedName, string compoundIdPartName,
            bool requires360Movement = false, bool containsRotationEvents = false, int sortingOrder = 99)
        {
            var newChar = ScriptableObject.CreateInstance<BeatmapCharacteristicSO>();

            newChar.SetField("_icon", icon);
            newChar.SetField("_descriptionLocalizationKey", hintText);
            newChar.SetField("_serializedName", serializedName);
            newChar.SetField("_characteristicNameLocalizationKey", characteristicName);
            newChar.SetField("_compoundIdPartName", compoundIdPartName);
            newChar.SetField("_requires360Movement", requires360Movement);
            newChar.SetField("_containsRotationEvents", containsRotationEvents);
            newChar.SetField("_sortingOrder", sortingOrder);

            if (_customCharacteristics.All(x => x.serializedName != newChar.serializedName))
            {
                _customCharacteristics.Add(newChar);
                return newChar;
            }

            return null;
        }

        public static SeperateSongFolder AddSeperateSongFolder(string name, string folderPath, FolderLevelPack pack, Sprite? image = null, bool wip = false, bool cachezips = false)
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
                    Logging.Logger.Error($"Failed to make folder for: {name}");
                    Logging.Logger.Error(ex);
                }
            }

            var entry = new SongFolderEntry(name, folderPath, pack, "", wip, cachezips);
            var seperateSongFolder = new ModSeperateSongFolder(entry, image == null ? UI.BasicUI.FolderIcon! : image);

            Loader.SeperateSongFolders.Add(seperateSongFolder);
            return seperateSongFolder;
        }


        public static void DeregisterizeCapability(string capability)
        {
            _capabilities.Remove(capability);
        }
    }
}