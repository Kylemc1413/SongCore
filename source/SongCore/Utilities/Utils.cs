using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SongCore.Data;
using UnityEngine;

namespace SongCore.Utilities
{
    public static class Utils
    {
        public static bool IsModInstalled(string modName)
        {
            return IPA.Loader.PluginManager.Plugins.Any(mod => mod.Name == modName) || IPA.Loader.PluginManager.EnabledPlugins.Any(mod => mod.Id == modName);
        }

        public static bool DiffHasColors(ExtraSongData.DifficultyData songData)
        {
            return songData._colorLeft != null || songData._colorRight != null || songData._envColorLeft != null || songData._envColorRight != null
                || songData._envColorLeftBoost != null || songData._envColorRightBoost != null || songData._obstacleColor != null;
        }

        public static Color ColorFromMapColor(Data.ExtraSongData.MapColor mapColor)
        {
            return new Color(mapColor.r, mapColor.g, mapColor.b);
        }

        public static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue)
        {
            if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
            {
                return defaultValue;
            }

            return (TEnum) Enum.Parse(typeof(TEnum), strEnumValue);
        }

        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public static string TrimEnd(this string text, string value)
        {
            return !text.EndsWith(value) ? text : text.Remove(text.LastIndexOf(value, StringComparison.Ordinal));
        }

        public static Sprite? LoadSpriteRaw(byte[] image, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureRaw(image), pixelsPerUnit);
        }

        public static Sprite? LoadSpriteFromFile(string filePath, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureFromFile(filePath), pixelsPerUnit);
        }

        public static Sprite? LoadSpriteFromTexture(Texture2D? spriteTexture, float pixelsPerUnit = 100.0f)
        {
            return spriteTexture != null ? Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit) : null;
        }

        public static Sprite? LoadSpriteFromResources(string resourcePath, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath), pixelsPerUnit);
        }

        public static byte[] GetResource(Assembly asm, string resourceName)
        {
            using var stream = asm.GetManifestResourceStream(resourceName)!;
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int) stream.Length);
            return data;
        }

        public static Texture2D? LoadTextureFromFile(string filePath)
        {
            return File.Exists(filePath) ? LoadTextureRaw(File.ReadAllBytes(filePath)) : null;
        }

        public static Texture2D? LoadTextureFromResources(string resourcePath)
        {
            return LoadTextureRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath));
        }

        public static Texture2D? LoadTextureRaw(byte[] file)
        {
            if (file.Length <= 0)
            {
                return null;
            }

            Texture2D tex2D = new Texture2D(2, 2);
            return tex2D.LoadImage(file) ? tex2D : null;
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
    }
}