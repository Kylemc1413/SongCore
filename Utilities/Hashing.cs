using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ProtoBuf;

namespace SongCore.Utilities
{
    public class Hashing
    {
        internal static ConcurrentDictionary<string, SongHashData> cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
        internal static ConcurrentDictionary<string, AudioCacheData> cachedAudioData = new ConcurrentDictionary<string, AudioCacheData>();
        public static readonly string cachedHashDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "SongCore", "SongHashData.bin");
        public static readonly string cachedAudioDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "SongCore", "SongDurationCache.bin");

        public static void ReadCachedSongHashes()
        {
            if (File.Exists(cachedHashDataPath))
            {
                using (var cachedSongHashDataStream = File.OpenRead(cachedHashDataPath))
                {
                    cachedSongHashData = Serializer.Deserialize<ConcurrentDictionary<string, SongHashData>>(cachedSongHashDataStream) ?? new ConcurrentDictionary<string, SongHashData>();
                }

                Logging.Logger.Info($"Finished reading cached hashes for {cachedSongHashData.Count} songs!");
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
            foreach (var hashData in cachedSongHashData.ToArray())
            {
                if (!currentSongPaths.Contains(hashData.Key))
                {
                    cachedSongHashData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached hashes for {cachedSongHashData.Count} songs!");

            using var cachedSongHashDataStream = File.Create(cachedHashDataPath);
            Serializer.Serialize(cachedSongHashDataStream, cachedSongHashData);
        }

        public static void ReadCachedAudioData()
        {
            if (File.Exists(cachedAudioDataPath))
            {
                using (var cachedAudioDataStream = File.OpenRead(cachedAudioDataPath))
                {
                    cachedAudioData = Serializer.Deserialize<ConcurrentDictionary<string, AudioCacheData>>(cachedAudioDataStream) ?? new ConcurrentDictionary<string, AudioCacheData>();
                }

                Logging.Logger.Info($"Finished reading cached Durations for {cachedAudioData.Count} songs!");
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
            foreach (var hashData in cachedAudioData.ToArray())
            {
                if (!currentSongPaths.Contains(hashData.Key))
                {
                    cachedAudioData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached Map Lengths for {cachedAudioData.Count} songs!");

            using var cachedAudioDataStream = File.Create(cachedAudioDataPath);
            Serializer.Serialize(cachedAudioDataStream, cachedAudioData);
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
            if (cachedSongHashData.TryGetValue(customLevelPath, out var cachedSong) && cachedSong.directoryHash == directoryHash)
            {
                cachedSongHash = cachedSong.songHash;
                return true;
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
            combinedBytes.AddRange(File.ReadAllBytes(Path.Combine(level.customLevelPath, "info.dat")));
            for (var i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (var i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    var beatmapPath = Path.Combine(level.customLevelPath, level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename);
                    if (File.Exists(beatmapPath))
                    {
                        combinedBytes.AddRange(File.ReadAllBytes(beatmapPath));
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

            byte[] combinedBytes = Array.Empty<byte>();
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(Path.Combine(customLevelPath, "info.dat"))).ToArray();
            for (var i = 0; i < level.difficultyBeatmapSets.Length; i++)
            {
                for (var i2 = 0; i2 < level.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    var beatmapPath = Path.Combine(customLevelPath, level.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename);
                    if (File.Exists(beatmapPath))
                    {
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
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

            byte[] combinedBytes = Array.Empty<byte>();
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(Path.Combine(level.customLevelPath, "info.dat"))).ToArray();
            for (var i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (var i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                {
                    var beatmapPath = Path.Combine(level.customLevelPath, level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename);
                    if (File.Exists(beatmapPath))
                    {
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
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
            using var sha1 = SHA1.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = sha1.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public static string CreateSha1FromBytes(byte[] input)
        {
            // Use input string to calculate MD5 hash
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(input);

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public static bool CreateSha1FromFile(string path, out string hash)
        {
            hash = string.Empty;
            if (!File.Exists(path))
            {
                return false;
            }

            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(path);
            var hashBytes = sha1.ComputeHash(stream);
            hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return true;
        }
    }
}