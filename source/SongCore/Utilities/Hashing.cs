using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BeatmapLevelSaveDataVersion4;
using Newtonsoft.Json;

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
                try
                {
                    var songHashData = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongHashData>>(File.ReadAllText(cachedHashDataPath));
                    if (songHashData != null)
                    {
                        cachedSongHashData = songHashData;
                        Plugin.Log.Info($"Finished loading cached hashes for {cachedSongHashData.Count} songs.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Error loading cached song hashes: {ex.Message}");
                    Plugin.Log.Error(ex);
                }
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
            foreach (var levelPath in cachedSongHashData.Keys)
            {
                var absolutePath = GetAbsolutePath(levelPath);
                if (!currentSongPaths.Contains(absolutePath) || (absolutePath == levelPath && IsInInstallPath(levelPath)))
                {
                    cachedSongHashData.TryRemove(levelPath, out _);
                }
            }

            try
            {
                Plugin.Log.Info($"Saving cached hashes for {cachedSongHashData.Count} songs.");
                File.WriteAllText(cachedHashDataPath, JsonConvert.SerializeObject(cachedSongHashData));
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error saving cached song hashes: {ex.Message}");
                Plugin.Log.Error(ex);
            }
        }

        public static void ReadCachedAudioData()
        {
            if (File.Exists(cachedAudioDataPath))
            {
                try
                {
                    var audioData = JsonConvert.DeserializeObject<ConcurrentDictionary<string, AudioCacheData>>(File.ReadAllText(cachedAudioDataPath));
                    if (audioData != null)
                    {
                        cachedAudioData = audioData;
                        Plugin.Log.Info($"Finished loading cached durations for {cachedAudioData.Count} songs.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Error loading cached song durations: {ex.Message}");
                    Plugin.Log.Error(ex);
                }
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
            foreach (var levelPath in cachedAudioData.Keys)
            {
                var absolutePath = GetAbsolutePath(levelPath);
                if (!currentSongPaths.Contains(absolutePath) || (absolutePath == levelPath && IsInInstallPath(levelPath)))
                {
                    cachedAudioData.TryRemove(levelPath, out _);
                }
            }

            try
            {
                Plugin.Log.Info($"Saving cached durations for {cachedAudioData.Count} songs.");
                File.WriteAllText(cachedAudioDataPath, JsonConvert.SerializeObject(cachedAudioData));
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error saving cached song durations: {ex.Message}");
                Plugin.Log.Error(ex);
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

            TryGetRelativePath(customLevelPath, out var relativePath);
            if (cachedSongHashData.TryGetValue(relativePath, out var cachedSong) && cachedSong.directoryHash == directoryHash)
            {
                cachedSongHash = cachedSong.songHash;
                return true;
            }

            cachedSongHash = string.Empty;
            return false;
        }

        [Obsolete("If your intent is to hash the custom level, use ComputeCustomLevelHash. Otherwise, use Collections.GetCustomLevelHash.", true)]
        public static string GetCustomLevelHash(BeatmapLevel level)
        {
            var hash = string.Empty;

            if (Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(level.levelID, out var loadedSaveData))
            {
                if (loadedSaveData.standardLevelInfoSaveData != null)
                {
                    hash = ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                }
                else if (loadedSaveData.beatmapLevelSaveData != null)
                {
                    hash = ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                }
            }

            return hash;
        }

        [Obsolete("If your intent is to hash the custom level, use ComputeCustomLevelHash. Otherwise, use Collections.GetCustomLevelHash.", true)]
        public static string GetCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, StandardLevelInfoSaveData standardLevelInfoSaveData)
        {
            return ComputeCustomLevelHash(customLevelFolderInfo, standardLevelInfoSaveData);
        }

        [Obsolete("If your intent is to hash the custom level, use ComputeCustomLevelHash. Otherwise, use Collections.GetCustomLevelHash.", true)]
        public static string GetCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, BeatmapLevelSaveData beatmapLevelSaveData)
        {
            return ComputeCustomLevelHash(customLevelFolderInfo, beatmapLevelSaveData);
        }

        public static string ComputeCustomLevelHash(BeatmapLevel level)
        {
            var hash = string.Empty;

            if (Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(level.levelID, out var loadedSaveData))
            {
                if (loadedSaveData.standardLevelInfoSaveData != null)
                {
                    hash = ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                }
                else if (loadedSaveData.beatmapLevelSaveData != null)
                {
                    hash = ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                }
            }

            return hash;
        }

        public static string ComputeCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, StandardLevelInfoSaveData standardLevelInfoSaveData)
        {
            if (GetCachedSongData(customLevelFolderInfo.folderPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var prependBytes = Encoding.UTF8.GetBytes(customLevelFolderInfo.levelInfoJsonString);
            var files = standardLevelInfoSaveData.difficultyBeatmapSets
                .SelectMany(difficultyBeatmapSet => difficultyBeatmapSet.difficultyBeatmaps)
                .Select(difficultyBeatmap => Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapFilename))
                .Where(File.Exists);

            string hash = CreateSha1FromFilesWithPrependBytes(prependBytes, files);
            TryGetRelativePath(customLevelFolderInfo.folderPath, out var relativePath);
            cachedSongHashData[relativePath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string ComputeCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, BeatmapLevelSaveData beatmapLevelSaveData)
        {
            if (GetCachedSongData(customLevelFolderInfo.folderPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var prependBytes = Encoding.UTF8.GetBytes(customLevelFolderInfo.levelInfoJsonString);
            var audioDataPath = Path.Combine(customLevelFolderInfo.folderPath, beatmapLevelSaveData.audio.audioDataFilename);
            var files = beatmapLevelSaveData.difficultyBeatmaps.SelectMany(difficultyBeatmap => new[]
            {
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapDataFilename),
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.lightshowDataFilename)
            }).Prepend(audioDataPath).Where(File.Exists);

            string hash = CreateSha1FromFilesWithPrependBytes(prependBytes, files);
            TryGetRelativePath(customLevelFolderInfo.folderPath, out var relativePath);
            cachedSongHashData[relativePath] = new SongHashData(directoryHash, hash);
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

        public static bool TryGetRelativePath(string path, out string relativePath)
        {
            var fromPath = IPA.Utilities.UnityGame.InstallPath;

            if (!fromPath.EndsWith(Path.DirectorySeparatorChar))
            {
                fromPath += Path.DirectorySeparatorChar;
            }

            if (!path.StartsWith(fromPath, StringComparison.Ordinal))
            {
                relativePath = path;
                return false;
            }

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(path);

            relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());

            if (!relativePath.StartsWith(".", StringComparison.Ordinal))
            {
                relativePath = Path.Combine(".", relativePath);
            }

            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return true;
        }


        public static bool IsInInstallPath(string path)
        {
            string fromPath = IPA.Utilities.UnityGame.InstallPath;

            if (!fromPath.EndsWith(Path.DirectorySeparatorChar))
            {
                fromPath += Path.DirectorySeparatorChar;
            }

            return path.StartsWith(fromPath, StringComparison.Ordinal);
        }

        // Black magic https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437
        private static string ByteToHexBitFiddle(byte[] bytes)
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


        private static string CreateSha1FromFilesWithPrependBytes(byte[] prependBytes, IEnumerable<string> files)
        {
            using var sha1 = SHA1.Create();
            var buffer = new byte[8192];

            sha1.TransformBlock(prependBytes, 0, prependBytes.Length, null, 0);

            foreach (var file in files)
            {
                using var fileStream = File.Open(file, FileMode.Open);
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }

            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return ByteToHexBitFiddle(sha1.Hash);
        }
    }
}