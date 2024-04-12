using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BeatmapLevelSaveDataVersion4;

namespace SongCore.Utilities
{
    public class Hashing
    {
        internal static ConcurrentDictionary<string, SongHashData> cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
        internal static ConcurrentDictionary<string, AudioCacheData> cachedAudioData = new ConcurrentDictionary<string, AudioCacheData>();
        public static readonly string cachedHashDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, nameof(SongCore), "SongHashData.dat");
        public static readonly string cachedAudioDataPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, nameof(SongCore), "SongDurationCache.dat");

        public static void ReadCachedSongHashes()
        {
            if (File.Exists(cachedHashDataPath))
            {
                cachedSongHashData = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongHashData>>(File.ReadAllText(cachedHashDataPath));
                if (cachedSongHashData == null)
                {
                    cachedSongHashData = new ConcurrentDictionary<string, SongHashData>();
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
                if (!currentSongPaths.Contains(GetAbsolutePath(hashData.Key)) || (GetAbsolutePath(hashData.Key) == hashData.Key && IsInInstallPath(hashData.Key)))
                {
                    cachedSongHashData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached hashes for {cachedSongHashData.Count} songs!");
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
                if (!currentSongPaths.Contains(GetAbsolutePath(hashData.Key)) || (GetAbsolutePath(hashData.Key) == hashData.Key && IsInInstallPath(hashData.Key)))
                {
                    cachedAudioData.TryRemove(hashData.Key, out _);
                }
            }

            Logging.Logger.Info($"Updating cached Map Lengths for {cachedAudioData.Count} songs!");
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

            if (cachedSongHashData.TryGetValue(GetRelativePath(customLevelPath), out var cachedSong) && cachedSong.directoryHash == directoryHash)
            {
                cachedSongHash = cachedSong.songHash;
                return true;
            }

            cachedSongHash = string.Empty;
            return false;
        }

        public static string GetCustomLevelHash(BeatmapLevel level)
        {
            var hash = string.Empty;

            var loadedSaveData = Loader.CustomLevelLoader._loadedBeatmapSaveData[level.levelID];
            if (loadedSaveData.standardLevelInfoSaveData != null)
            {
                hash = GetCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
            }
            else if (loadedSaveData.beatmapLevelSaveData != null)
            {
                hash = GetCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
            }

            return hash;
        }

        [Obsolete("Use the other overloads.", true)]
        public static string GetCustomLevelHash(StandardLevelInfoSaveData level, string customLevelPath)
        {
            var infoFilePath = Path.Combine(customLevelPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(infoFilePath))
            {
                return string.Empty;
            }

            var customLevelInfo = new CustomLevelFolderInfo(customLevelPath, string.Empty, File.ReadAllText(infoFilePath));
            return GetCustomLevelHash(customLevelInfo, level);
        }

        [Obsolete("Use the other overloads.", true)]
        public static string GetCustomLevelHash(BeatmapLevelSaveData level, string customLevelPath)
        {
            var infoFilePath = Path.Combine(customLevelPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(infoFilePath))
            {
                return string.Empty;
            }

            var customLevelInfo = new CustomLevelFolderInfo(customLevelPath, string.Empty, File.ReadAllText(infoFilePath));
            return GetCustomLevelHash(customLevelInfo, level);
        }

        public static string GetCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, StandardLevelInfoSaveData standardLevelInfoSaveData)
        {
            if (GetCachedSongData(customLevelFolderInfo.folderPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var prependBytes = BeatmapLevelDataUtils.kUtf8Encoding.GetBytes(customLevelFolderInfo.levelInfoJsonString);
            var files = standardLevelInfoSaveData.difficultyBeatmapSets
                .SelectMany(difficultyBeatmapSet => difficultyBeatmapSet.difficultyBeatmaps)
                .Select(difficultyBeatmap => Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapFilename))
                .Where(File.Exists);

            string hash = CreateSha1FromFilesWithPrependBytes(prependBytes, files);
            cachedSongHashData[GetRelativePath(customLevelFolderInfo.folderPath)] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, BeatmapLevelSaveData beatmapLevelSaveData)
        {
            if (GetCachedSongData(customLevelFolderInfo.folderPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var prependBytes = BeatmapLevelDataUtils.kUtf8Encoding.GetBytes(customLevelFolderInfo.levelInfoJsonString);
            var audioDataPath = Path.Combine(customLevelFolderInfo.folderPath, beatmapLevelSaveData.audio.audioDataFilename);
            var files = beatmapLevelSaveData.difficultyBeatmaps.SelectMany(difficultyBeatmap => new[]
            {
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapDataFilename),
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.lightshowDataFilename)
            }).Prepend(audioDataPath).Where(File.Exists);

            string hash = CreateSha1FromFilesWithPrependBytes(prependBytes, files);
            cachedSongHashData[GetRelativePath(customLevelFolderInfo.folderPath)] = new SongHashData(directoryHash, hash);
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

        public static string CreateSha1FromFilesWithPrependBytes(IEnumerable<byte> prependBytes, IEnumerable<string> files)
        {
            using var sha1 = SHA1.Create();
            var buffer = new byte[4096];
            var bufferIndex = 0;

            foreach (var prependByte in prependBytes)
            {
                buffer[bufferIndex++] = prependByte;
                if (bufferIndex == buffer.Length)
                {
                    sha1.TransformBlock(buffer, 0, buffer.Length, null, 0);
                    bufferIndex = 0;
                }
            }

            foreach (var file in files)
            {
                using var fileStream = File.Open(file, FileMode.Open);
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, bufferIndex, buffer.Length - bufferIndex)) > 0)
                {
                    bufferIndex += bytesRead;
                    if (bufferIndex == buffer.Length)
                    {
                        sha1.TransformBlock(buffer, 0, buffer.Length, null, 0);
                        bufferIndex = 0;
                    }
                }
            }

            sha1.TransformFinalBlock(buffer, 0, bufferIndex);

            return ByteToHexBitFiddle(sha1.Hash);
        }
    }
}