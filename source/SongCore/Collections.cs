using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BGLib.JsonExtension;
using IPA.Utilities;
using SongCore.Data;
using UnityEngine;

namespace SongCore
{
    public static class Collections
    {
        private static readonly List<string> _capabilities = new List<string>();
        private static readonly List<BeatmapCharacteristicSO> _customCharacteristics = new List<BeatmapCharacteristicSO>();

        private const int kCustomCharacteristicBaseValue = 1000;
        private static readonly Dictionary<string, BeatmapCharacteristic> _customCharacteristicsBySerializedName = new Dictionary<string, BeatmapCharacteristic>();
        private static readonly Dictionary<BeatmapCharacteristic, BeatmapCharacteristicSO> _customCharacteristicScriptableObjects = new Dictionary<BeatmapCharacteristic, BeatmapCharacteristicSO>();

        internal static readonly string DataPath = Path.Combine(UnityGame.UserDataPath, nameof(SongCore), "SongCoreExtraData.dat");
        internal static readonly ConcurrentDictionary<string, string> LevelHashDictionary = new ConcurrentDictionary<string, string>();
        internal static readonly ConcurrentDictionary<string, List<string>> HashLevelDictionary = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        internal static BeatmapLevelPack? WipLevelPack;
        internal static ConcurrentDictionary<string, SongData> CustomSongsData = new ConcurrentDictionary<string, SongData>();

        public static ReadOnlyCollection<string> capabilities => _capabilities.AsReadOnly();
        public static ReadOnlyCollection<BeatmapCharacteristicSO> customCharacteristics => _customCharacteristics.AsReadOnly();

        public static bool songWithHashPresent(string hash)
        {
            return HashLevelDictionary.ContainsKey(hash);
        }

        // TODO: Replace by better naming.
        public static List<string> levelIDsForHash(string hash)
        {
            return HashLevelDictionary.TryGetValue(hash, out var songs) ? songs : new List<string>();
        }

        public static string GetCustomLevelHash(string levelID)
        {
            return LevelHashDictionary.TryGetValue(levelID, out var hash) ? hash : string.Empty;
        }

        public static SongData? GetCustomLevelSongData(string levelID)
        {
            return CustomSongsData.GetValueOrDefault(levelID);
        }

        internal static void CreateCustomLevelSongData(string levelID, CustomLevelLoader.LoadedSaveData loadedSaveData)
        {
            var extraSongData = new SongData();
            if (CustomSongsData.TryAdd(levelID, extraSongData))
            {
                extraSongData.PopulateFromLoadedSaveData(loadedSaveData);
            }
        }

        public static SongData.DifficultyData? GetCustomLevelSongDifficultyData(BeatmapKey beatmapKey)
        {
            SongData? songData = null;

            if (beatmapKey.levelId.StartsWith(CustomLevelLoader.kCustomLevelPrefixId, StringComparison.Ordinal))
            {
                // TODO: Will be null in the editor due to levelID being "custom_level_CustomLevel".
                songData = GetCustomLevelSongData(beatmapKey.levelId);
            }

            var diffData = songData?._difficulties.FirstOrDefault(x =>
                x._difficulty == beatmapKey.difficulty && (x._beatmapCharacteristicName == beatmapKey.characteristic.NameLocalizationKey() ||
                                                           x._beatmapCharacteristicName == beatmapKey.characteristic.SerializedName()));

            return diffData;
        }

        internal static async Task LoadCachedSongDataAsync()
        {
            if (!File.Exists(DataPath))
            {
                return;
            }

            try
            {
                var songData = await Task.Run(() => JsonFileHandler.ReadFromFile<ConcurrentDictionary<string, SongData>>(DataPath));
                if (songData != null)
                {
                    CustomSongsData = songData;
                    Plugin.Log.Info($"Finished loading cached song data for {CustomSongsData.Count} songs.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error loading cached song data: {ex.Message}");
                Plugin.Log.Error(ex);
            }
        }

        internal static async Task SaveCachedSongDataAsync()
        {
            Plugin.Log.Info($"Saving cached song data for {CustomSongsData.Count} songs.");

            try
            {
                await Task.Run(() => JsonFileHandler.WriteCompactWithoutDefault(CustomSongsData, DataPath));
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error saving cached song data: {ex.Message}");
                Plugin.Log.Error(ex);
            }
        }

        public static BeatmapCharacteristicSO? RegisterCustomCharacteristic(Sprite icon, string characteristicName, string hintText, string serializedName, string compoundIdPartName,
            bool requires360Movement = false, bool containsRotationEvents = false, int sortingOrder = 99)
        {
            var newChar = ScriptableObject.CreateInstance<BeatmapCharacteristicSO>();

            newChar._icon = icon;
            newChar._descriptionLocalizationKey = hintText;
            newChar._serializedName = serializedName;
            newChar._characteristicNameLocalizationKey = characteristicName;
            newChar._compoundIdPartName = compoundIdPartName;
            newChar._requires360Movement = requires360Movement;
            newChar._containsRotationEvents = containsRotationEvents;
            newChar._sortingOrder = sortingOrder;

            newChar.name = serializedName + "BeatmapCharacteristic";

            if (_customCharacteristics.All(x => x.serializedName != newChar.serializedName))
            {
                var characteristic = (BeatmapCharacteristic)(kCustomCharacteristicBaseValue + _customCharacteristics.Count);
                _customCharacteristics.Add(newChar);
                _customCharacteristicsBySerializedName.Add(serializedName, characteristic);
                _customCharacteristicScriptableObjects.Add(characteristic, newChar);
                return newChar;
            }

            return null;
        }

        internal static bool TryGetCustomCharacteristic(string? serializedName, out BeatmapCharacteristic characteristic)
        {
            characteristic = default;
            return serializedName != null && _customCharacteristicsBySerializedName.TryGetValue(serializedName, out characteristic);
        }

        internal static bool TryGetCustomCharacteristicData(BeatmapCharacteristic characteristic, out BeatmapCharacteristicSO data)
        {
            return _customCharacteristicScriptableObjects.TryGetValue(characteristic, out data);
        }

        public static SeparateSongFolder AddSeparateSongFolder(string name, string folderPath, FolderLevelPack pack, Sprite? image = null, bool wip = false, bool cachezips = false)
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
                    Plugin.Log.Error($"Failed to make folder for: {name}");
                    Plugin.Log.Error(ex);
                }
            }

            var entry = new SongFolderEntry(name, folderPath, pack, "", wip, cachezips);
            var separateSongFolder = new ModSeparateSongFolder(entry, image == null ? UI.BasicUI.FolderIcon! : image);

            Loader.SeparateSongFolders.Add(separateSongFolder);
            return separateSongFolder;
        }

        public static void RegisterCapability(string capability)
        {
            if (!_capabilities.Contains(capability))
            {
                _capabilities.Add(capability);
            }
        }

        public static void DeregisterCapability(string capability)
        {
            _capabilities.Remove(capability);
        }
    }
}
