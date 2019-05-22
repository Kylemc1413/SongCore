using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace SongCore.Utilities
{
    public static class Utils
    {

        public static string GetCustomLevelHash(CustomPreviewBeatmapLevel level)
        {
            List<byte> combinedBytes = new List<byte>();
            combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat"));
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                    {
                        combinedBytes.AddRange(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename));
                        Logging.Log(level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].difficulty + " " + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].beatmapCharacteristicName);
                    }
            }
            Logging.Log("Hash done");
            return Utils.CreateSha1FromBytes(combinedBytes.ToArray());

        }

        public static string GetCustomLevelHash(CustomBeatmapLevel level)
        {
            byte[] combinedBytes = new byte[0];
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + "info.dat")).ToArray();
            for (int i = 0; i < level.standardLevelInfoSaveData.difficultyBeatmapSets.Length; i++)
            {
                for (int i2 = 0; i2 < level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps.Length; i2++)
                    if (File.Exists(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(level.customLevelPath + '/' + level.standardLevelInfoSaveData.difficultyBeatmapSets[i].difficultyBeatmaps[i2].beatmapFilename)).ToArray();
            }

            return Utils.CreateSha1FromBytes(combinedBytes);

        }


        public static bool IsModInstalled(string ModName)
        {
            //       Logging.Log($"Checking for Mod: {ModName}");
            foreach (var mod in IPA.Loader.PluginManager.Plugins)
            {
                //        Logging.Log($"Comparing to: {mod.Name}");
                if (mod.Name == ModName)
                    return true;
            }
            foreach (var mod in IPA.Loader.PluginManager.AllPlugins)
            {
                //         Logging.Log($"Comparing to: {mod.Metadata.Id}");
                if (mod.Metadata.Id == ModName)
                    return true;
            }
            return false;
        }

        public static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue)
        {
            if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
                return defaultValue;

            return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        }

        public static string CreateSha1FromString(string input)
        {
            // Use input string to calculate MD5 hash
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = sha1.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string CreateSha1FromBytes(byte[] input)
        {
            // Use input string to calculate MD5 hash
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = input;
                var hashBytes = sha1.ComputeHash(inputBytes);
                return string.Concat(hashBytes.Select(b => b.ToString("x2")));
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

                    // Convert the byte array to hexadecimal string
                    var sb = new StringBuilder();
                    foreach (var hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }
        public static string TrimEnd(this string text, string value)
        {
            if (!text.EndsWith(value))
                return text;

            return text.Remove(text.LastIndexOf(value));
        }

        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition)
        {
            return CreateText(parent, text, anchoredPosition, new Vector2(60f, 10f));
        }

        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            TextMeshProUGUI textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            textMesh.font = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        public static Sprite LoadSpriteRaw(byte[] image, float PixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureRaw(image), PixelsPerUnit);
        }

        public static Sprite LoadSpriteFromTexture(Texture2D SpriteTexture, float PixelsPerUnit = 100.0f)
        {
            if (SpriteTexture)
                return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);
            return null;
        }

        public static Sprite LoadSpriteFromFile(string FilePath, float PixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureFromFile(FilePath), PixelsPerUnit);
        }

        public static Sprite LoadSpriteFromResources(string resourcePath, float PixelsPerUnit = 100.0f)
        {
            return LoadSpriteRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath), PixelsPerUnit);
        }

        public static byte[] GetResource(Assembly asm, string ResourceName)
        {
            System.IO.Stream stream = asm.GetManifestResourceStream(ResourceName);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }

        public static void PrintHierarchy(Transform transform, string spacing = "|-> ")
        {
            spacing = spacing.Insert(1, "  ");
            var tempList = transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                Console.WriteLine($"{spacing}{child.name}");
                PrintHierarchy(child, "|" + spacing);
            }
        }

        public static Texture2D LoadTextureRaw(byte[] file)
        {
            if (file.Count() > 0)
            {
                Texture2D Tex2D = new Texture2D(2, 2);
                if (Tex2D.LoadImage(file))
                    return Tex2D;
            }
            return null;
        }

        public static Texture2D LoadTextureFromFile(string FilePath)
        {
            if (File.Exists(FilePath))
                return LoadTextureRaw(File.ReadAllBytes(FilePath));

            return null;
        }

        public static Texture2D LoadTextureFromResources(string resourcePath)
        {
            return LoadTextureRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath));
        }

    }
}