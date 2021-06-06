using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SongCore.Utilities
{
    public class Hashing
    {
        internal static ConcurrentDictionary<string, SongHashData> cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
        internal static ConcurrentDictionary<string, AudioCacheData> cachedAudioData = new ConcurrentDictionary<string, AudioCacheData>();
        public static readonly string cachedHashDataPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "UserData", "SongCore", "SongHashData.dat");
        public static readonly string cachedAudioDataPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "UserData", "SongCore", "SongDurationCache.dat");

        public static void ReadCachedSongHashes()
        {
            if (File.Exists(cachedHashDataPath))
            {
                cachedSongHashData = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongHashData>>(File.ReadAllText(cachedHashDataPath));
                if (cachedSongHashData == null)
                {
                    cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
                }

                Logging.Log($"Finished reading cached hashes for {cachedSongHashData.Count} songs!");
            }
        }

        public static void UpdateCachedHashes(HashSet<string> currentSongPaths)
        {
            UpdateCachedHashesInternal(currentSongPaths);
        }

        /// <summary>
        /// Intended for use in the Loader
        /// </summary>
        /// <param name="currentSongPaths"></param>
        internal static void UpdateCachedHashesInternal(ICollection<string> currentSongPaths)
        {
            foreach (KeyValuePair<string, SongHashData> hashData in cachedSongHashData.ToArray())
            {
                if (!currentSongPaths.Contains(hashData.Key))
                {
                    cachedSongHashData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Log($"Updating cached hashes for {cachedSongHashData.Count} songs!");
            File.WriteAllText(cachedHashDataPath, Newtonsoft.Json.JsonConvert.SerializeObject(cachedSongHashData));
        }

        public static void ReadCachedAudioData()
        {
            if (File.Exists(cachedAudioDataPath))
            {
                cachedAudioData = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, AudioCacheData>>(File.ReadAllText(cachedAudioDataPath));
                if (cachedAudioData == null)
                {
                    cachedAudioData = new ConcurrentDictionary<string, AudioCacheData>();
                }

                Logging.Log($"Finished reading cached Durations for {cachedAudioData.Count} songs!");
            }
        }

        public static void UpdateCachedAudioData(HashSet<string> currentSongPaths)
        {
            UpdateCachedAudioDataInternal(currentSongPaths);
        }

        /// <summary>
        /// Intended for use in the Loader
        /// </summary>
        /// <param name="currentSongPaths"></param>
        internal static void UpdateCachedAudioDataInternal(ICollection<string> currentSongPaths)
        {
            foreach (KeyValuePair<string, AudioCacheData> hashData in cachedAudioData.ToArray())
            {
                if (!currentSongPaths.Contains(hashData.Key))
                {
                    cachedAudioData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Log($"Updating cached Map Lengths for {cachedAudioData.Count} songs!");
            File.WriteAllText(cachedAudioDataPath, Newtonsoft.Json.JsonConvert.SerializeObject(cachedAudioData));
        }

        private static long GetDirectoryHash(string directory)
        {
            long hash = 0;
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            foreach (FileInfo f in directoryInfo.GetFiles())
            {
                hash ^= f.CreationTimeUtc.ToFileTimeUtc();
                hash ^= f.LastWriteTimeUtc.ToFileTimeUtc();
                hash ^= f.Name.GetHashCode();
                hash ^= f.Length;
            }
            return hash;
        }

        private static bool GetCachedSongData(string customLevelPath, out long directoryHash, out string cachedSongHash)
        {
            directoryHash = GetDirectoryHash(customLevelPath);
            cachedSongHash = string.Empty;
            if (cachedSongHashData.TryGetValue(customLevelPath, out var cachedSong))
            {
                if (cachedSong.directoryHash == directoryHash)
                {
                    cachedSongHash = cachedSong.songHash;
                    return true;
                }
            }
            return false;
        }

        public static string GetCustomLevelHash(CustomPreviewBeatmapLevel level)
        {
            if (GetCachedSongData(level.customLevelPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            List<byte> combinedBytes = new List<byte>();
            combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat"));
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                    {
                        combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename));
                    }
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[level.customLevelPath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetCustomLevelHash(StandardLevelInfoSaveData level, string customLevelPath)
        {
            if (GetCachedSongData(customLevelPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            byte[] combinedBytes = new byte[0];
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(customLevelPath + '/' + "info.dat")).ToArray();
            for (int i = 0; i < level.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    if (File.Exists(customLevelPath + '/' + level.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                    {
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(customLevelPath + '/' + level.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename)).ToArray();
                    }
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[customLevelPath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetCustomLevelHash(CustomBeatmapLevel level)
        {
            if (GetCachedSongData(level.customLevelPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            byte[] combinedBytes = new byte[0];
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat")).ToArray();
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                    {
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename)).ToArray();
                    }
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[level.customLevelPath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string CreateSha1FromString(string input)
        {
            // Use input string to calculate MD5 hash
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = sha1.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }

        public static string CreateSha1FromBytes(byte[] input)
        {
            // Use input string to calculate MD5 hash
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = input;
                var hashBytes = sha1.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }

        public static bool CreateSha1FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path))
            {
                return false;
            }

            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var hashBytes = sha1.ComputeHash(stream);
                    hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                    return true;
                }
            }
        }

    }
}
