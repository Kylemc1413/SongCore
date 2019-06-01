using SongCore.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SongCore.Utilities
{
    class Hashing
    {
        internal static Dictionary<string, SongHashData> cachedSongHashData = new Dictionary<string, SongHashData>();
        internal static string cachedHashDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongHashData.dat");
        public static void ReadCachedSongHashes()
        {
            if (File.Exists(cachedHashDataPath))
            {
                cachedSongHashData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, SongHashData>>(File.ReadAllText(cachedHashDataPath));
                Logging.Log($"Finished reading cached hashes for {cachedSongHashData.Count} songs!");
            }
        }

        public static void UpdateCachedHashes(HashSet<string> currentSongPaths)
        {
            foreach (KeyValuePair<string, SongHashData> hashData in cachedSongHashData.ToArray())
            {
                if (!currentSongPaths.Contains(hashData.Key))
                    cachedSongHashData.Remove(hashData.Key);
            }
            Logging.Log($"Updating cached hashes for {cachedSongHashData.Count} songs!");
            File.WriteAllText(cachedHashDataPath, Newtonsoft.Json.JsonConvert.SerializeObject(cachedSongHashData));
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
                return songHash;

            List<byte> combinedBytes = new List<byte>();
            combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat"));
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                    {
                        combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename));
                    }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[level.customLevelPath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetCustomLevelHash(StandardLevelInfoSaveData level, string customLevelPath)
        {
            if (GetCachedSongData(customLevelPath, out var directoryHash, out var songHash))
                return songHash;

            byte[] combinedBytes = new byte[0];
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(customLevelPath + '/' + "info.dat")).ToArray();
            for (int i = 0; i < level.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                    if (File.Exists(customLevelPath + '/' + level.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(customLevelPath + '/' + level.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename)).ToArray();
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            cachedSongHashData[customLevelPath] = new SongHashData(directoryHash, hash);
            return hash;
        }

        public static string GetCustomLevelHash(CustomBeatmapLevel level)
        {
            if (GetCachedSongData(level.customLevelPath, out var directoryHash, out var songHash))
                return songHash;

            byte[] combinedBytes = new byte[0];
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat")).ToArray();
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename)).ToArray();
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
            if (!File.Exists(path)) return false;
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
