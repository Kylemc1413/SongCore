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

        [Obsolete("Use GetCustomLevelHash instead.", true)]
        public static string hashForLevelID(string levelID)
        {
            return GetCustomLevelHash(levelID);
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

        [Obsolete("Get the loaded save data from CustomLevelLoader._loadedBeatmapSaveData.", true)]
        public static CustomLevelLoader.LoadedSaveData? GetLoadedSaveData(string levelID)
        {
            return Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(levelID, out var loadedSaveData) ? loadedSaveData : null;
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

        [Obsolete("Get the song data with GetCustomLevelSongData instead.", true)]
        public static ExtraSongData? RetrieveExtraSongData(string hash)
        {
            return GetCustomLevelSongData(CustomLevelLoader.kCustomLevelPrefixId + hash)?.ToExtraSongData();
        }

        [Obsolete("Get the song difficulty data with GetCustomLevelSongDifficultyData instead.", true)]
        public static ExtraSongData.DifficultyData? RetrieveDifficultyData(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
        {
            ExtraSongData? songData = null;

            if (!beatmapLevel.hasPrecalculatedData)
            {
                songData = RetrieveExtraSongData(GetCustomLevelHash(beatmapLevel.levelID));
            }

            var diffData = songData?._difficulties.FirstOrDefault(x =>
                x._difficulty == beatmapKey.difficulty && (x._beatmapCharacteristicName == beatmapKey.beatmapCharacteristic.characteristicNameLocalizationKey ||
                                                        x._beatmapCharacteristicName == beatmapKey.beatmapCharacteristic.serializedName));

            return diffData;
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
                x._difficulty == beatmapKey.difficulty && (x._beatmapCharacteristicName == beatmapKey.beatmapCharacteristic.characteristicNameLocalizationKey ||
                                                           x._beatmapCharacteristicName == beatmapKey.beatmapCharacteristic.serializedName));

            return diffData;
        }

        internal static void LoadCustomLevelSongData()
        {
            Task.Run(() =>
            {
                try
                {
                    using var reader = new JsonTextReader(new StreamReader(DataPath));
                    var serializer = JsonSerializer.CreateDefault();
                    var songData = serializer.Deserialize<ConcurrentDictionary<string, SongData>?>(reader);
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
            });
        }

        internal static async Task SaveCustomLevelSongDataAsync()
        {
            try
            {
                Plugin.Log.Info($"Saving cached song data for {CustomSongsData.Count} songs.");
                await using var writer = new StreamWriter(DataPath);
                await writer.WriteAsync(JsonConvert.SerializeObject(CustomSongsData, Formatting.None));
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
                _customCharacteristics.Add(newChar);
                return newChar;
            }

            return null;
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