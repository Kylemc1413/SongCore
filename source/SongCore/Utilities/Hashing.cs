using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace SongCore.Utilities
{
    public class Hashing
    {
        private static readonly MessagePackSerializerOptions serializerOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        internal static ConcurrentDictionary<string, SongHashData> cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
        internal static ConcurrentDictionary<string, AudioCacheData> cachedAudioData = new ConcurrentDictionary<string, AudioCacheData>();
        public static readonly string cachedHashDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, nameof(SongCore), "SongHashData.dat");
        public static readonly string cachedAudioDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, nameof(SongCore), "SongDurationCache.dat");

        [Obsolete("Use the async overload.", true)]
        public static void ReadCachedSongHashes()
        {
            ReadCachedSongHashesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public static async Task ReadCachedSongHashesAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(cachedHashDataPath))
            {
                try
                {
                    using var fileStream = File.Open(cachedHashDataPath, FileMode.Open);
                    cachedSongHashData = await MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<string, SongHashData>>(fileStream, serializerOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error($"Error loading cached song hashes: {ex.Message}");
                    Logging.Logger.Error(ex);
                }

                Logging.Logger.Info($"Finished reading cached hashes for {cachedSongHashData.Count} songs!");
            }
        }

        public static void UpdateCachedHashes(HashSet<string> currentSongPaths)
        {
            UpdateCachedHashesInternalAsync(currentSongPaths, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Intended for use in the Loader
        /// </summary>
        /// <param name="currentSongPaths"></param>
        /// <param name="cancellationToken"></param>
        internal static async Task UpdateCachedHashesInternalAsync(ICollection<string> currentSongPaths, CancellationToken cancellationToken)
        {
            foreach (var hashData in cachedSongHashData.ToArray())
            {
                if (!currentSongPaths.Contains(GetAbsolutePath(hashData.Key)) || (GetAbsolutePath(hashData.Key) == hashData.Key && IsInInstallPath(hashData.Key)))
                {
                    cachedSongHashData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached hashes for {cachedSongHashData.Count} songs!");

            try
            {
                using var fileStream = File.Open(cachedHashDataPath, FileMode.Create);
                await MessagePackSerializer.SerializeAsync(fileStream, cachedSongHashData, serializerOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error saving cached song hashes: {ex.Message}");
                Logging.Logger.Error(ex);
            }
        }

        [Obsolete("Use the async overload.", true)]
        public static void ReadCachedAudioData()
        {
            ReadCachedAudioDataAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public static async Task ReadCachedAudioDataAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(cachedAudioDataPath))
            {
                try
                {
                    using var fileStream = File.Open(cachedAudioDataPath, FileMode.Open);
                    cachedAudioData = await MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<string, AudioCacheData>>(fileStream, serializerOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error($"Error loading cached audio data: {ex.Message}");
                    Logging.Logger.Error(ex);
                }

                Logging.Logger.Info($"Finished reading cached durations for {cachedAudioData.Count} songs!");
            }
        }

        public static void UpdateCachedAudioData(HashSet<string> currentSongPaths)
        {
            UpdateCachedAudioDataInternalAsync(currentSongPaths, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Intended for use in the Loader
        /// </summary>
        /// <param name="currentSongPaths"></param>
        /// <param name="cancellationToken"></param>
        internal static async Task UpdateCachedAudioDataInternalAsync(ICollection<string> currentSongPaths, CancellationToken cancellationToken)
        {
            foreach (var hashData in cachedAudioData.ToArray())
            {
                if (!currentSongPaths.Contains(GetAbsolutePath(hashData.Key)) || (GetAbsolutePath(hashData.Key) == hashData.Key && IsInInstallPath(hashData.Key)))
                {
                    cachedAudioData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached map lengths for {cachedAudioData.Count} songs!");

            try
            {
                using var fileStream = File.Open(cachedAudioDataPath, FileMode.Create);
                await MessagePackSerializer.SerializeAsync(fileStream, cachedAudioData, serializerOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error saving cached audio data: {ex.Message}");
                Logging.Logger.Error(ex);
            }
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

            if (cachedSongHashData.TryGetValue(GetRelativePath(customLevelPath), out var cachedSong) && cachedSong.directoryHash == directoryHash)
            {
                cachedSongHash = cachedSong.songHash;
                return true;
            }

            cachedSongHash = string.Empty;
            return false;
        }

        public static string? GetCustomLevelHash(BeatmapLevel level)
        {
            var standardLevelInfoSaveData = Collections.GetStandardLevelInfoSaveData(level.levelID);
            if (standardLevelInfoSaveData == null)
            {
                return null;
            }

            var customLevelPath = Collections.GetCustomLevelPath(level.levelID);
            if (string.IsNullOrEmpty(customLevelPath))
            {
                return null;
            }

            return GetCustomLevelHash(customLevelPath, standardLevelInfoSaveData.difficultyBeatmapSets);
        }

        public static string GetCustomLevelHash(StandardLevelInfoSaveData level, string customLevelPath)
        {
            return GetCustomLevelHash(customLevelPath, level.difficultyBeatmapSets);
        }

        private static string GetCustomLevelHash(string levelPath, StandardLevelInfoSaveData.DifficultyBeatmapSet[] beatmapSets)
        {
            if (GetCachedSongData(levelPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var levelFolder = levelPath + Path.DirectorySeparatorChar;
            IEnumerable<byte> combinedBytes = File.ReadAllBytes(levelFolder + CustomLevelPathHelper.kStandardLevelInfoFilename);

            foreach(var beatmapSet in beatmapSets)
            {
                foreach(var difficultyBeatmap in beatmapSet.difficultyBeatmaps)
                {
                    var beatmapPath = levelFolder + difficultyBeatmap.beatmapFilename;
                    if (File.Exists(beatmapPath))
                    {
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath));
                    }
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[GetRelativePath(levelPath)] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetAbsolutePath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path.StartsWith("." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return Path.Combine(IPA.Utilities.UnityGame.InstallPath, path.Substring(2));
            }

            return path;
        }

        public static string GetRelativePath(string path)
        {
            string fromPath = IPA.Utilities.UnityGame.InstallPath;

            if (!fromPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fromPath += Path.DirectorySeparatorChar;
            }

            if(!path.StartsWith(fromPath, StringComparison.Ordinal)) return path;

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(path);

            string relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());

            if (!relativePath.StartsWith(".", StringComparison.Ordinal))
            {
                relativePath = Path.Combine(".", relativePath);
            }

            return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static bool IsInInstallPath(string path)
        {
            string fromPath = IPA.Utilities.UnityGame.InstallPath;

            if (!fromPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fromPath += Path.DirectorySeparatorChar;
            }

            return path.StartsWith(fromPath, StringComparison.Ordinal);
        }

        // Black magic https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437
        static string ByteToHexBitFiddle(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char) (55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char) (55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }

        public static string CreateSha1FromString(string input)
        {
            // Use input string to calculate MD5 hash
            using var sha1 = SHA1.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = sha1.ComputeHash(inputBytes);

            return ByteToHexBitFiddle(hashBytes);
        }

        public static string CreateSha1FromBytes(byte[] input)
        {
            // Use input string to calculate MD5 hash
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(input);

            return ByteToHexBitFiddle(hashBytes);
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
            hash = ByteToHexBitFiddle(hashBytes);
            return true;
        }
    }
}