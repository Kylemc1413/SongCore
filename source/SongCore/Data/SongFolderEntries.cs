using SongCore.OverrideClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Concurrent;
using SongCore.Utilities;

namespace SongCore.Data
{
    public enum FolderLevelPack { CustomLevels, CustomWIPLevels, NewPack, CachedWIPLevels };

    [Serializable]
    public class SongFolderEntry
    {
        public string Name;
        public string Path;
        public FolderLevelPack Pack;
        public string ImagePath;
        public bool WIP;
        public bool CacheZIPs;

        public SongFolderEntry(string name, string path, FolderLevelPack pack, string imagePath = "", bool wip = false, bool cachezips = false)
        {
            Name = name;
            Path = path;
            Pack = pack;
            ImagePath = imagePath;
            WIP = wip;
            CacheZIPs = cachezips;
        }
    }

    public class SeparateSongFolder
    {
        public readonly ConcurrentDictionary<string, BeatmapLevel> Levels = new ConcurrentDictionary<string, BeatmapLevel>();

        public SongFolderEntry SongFolderEntry { get; private set; }
        public SongCoreCustomBeatmapLevelPack LevelPack { get; private set; } = null;
        public SeparateSongFolder? CacheFolder { get; private set; }

        public SeparateSongFolder(SongFolderEntry folderEntry, SeparateSongFolder? cacheFolder = null)
        {
            SongFolderEntry = folderEntry;
            CacheFolder = cacheFolder;

            if (folderEntry.Pack == FolderLevelPack.NewPack)
            {
                var image = UI.BasicUI.FolderIcon!;

                if (!string.IsNullOrEmpty(folderEntry.ImagePath))
                {
                    try
                    {
                        var packImage = Utils.LoadSpriteFromFile(folderEntry.ImagePath);
                        if (packImage != null)
                        {
                            image = packImage;
                        }
                    }
                    catch
                    {
                        Logging.Logger.Info($"Failed to load image for separate folder \"{folderEntry.Name}\"");
                    }
                }

                LevelPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + folderEntry.Name, folderEntry.Name, image, Levels.Values.ToArray());
            }
        }

        public SeparateSongFolder(SongFolderEntry folderEntry, UnityEngine.Sprite image)
        {
            SongFolderEntry = folderEntry;
            if (folderEntry.Pack == FolderLevelPack.NewPack)
            {
                LevelPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + folderEntry.Name, folderEntry.Name, image, Levels.Values.ToArray());
            }
        }

        public static List<SeparateSongFolder> ReadSeparateFoldersFromFile(string filePath)
        {
            var result = new List<SeparateSongFolder>();
            try
            {
                XDocument file = XDocument.Load(filePath);
                foreach (var item in file.Root.Elements())
                {
                    //           Console.WriteLine("Element Name: " + item.Name);
                    string name = item.Element("Name").Value;
                    if (name == "Example")
                    {
                        continue;
                    }

                    string path = item.Element("Path").Value;
                    var pack = int.Parse(item.Element("Pack").Value);
                    string imagePath = "";
                    var image = item.Element("ImagePath");
                    if (image != null)
                    {
                        imagePath = image.Value;
                    }

                    var isWIP = false;
                    var wip = item.Element("WIP");
                    if (wip != null)
                    {
                        isWIP = bool.Parse(wip.Value);
                    }

                    var zipCaching = false;
                    var cachezips = item.Element("CacheZIPs");
                    if (cachezips != null)
                    {
                        zipCaching = bool.Parse(cachezips.Value);
                    }

                    SongFolderEntry entry = new SongFolderEntry(name, path, (FolderLevelPack) pack, imagePath, isWIP, zipCaching);
                    //   Console.WriteLine("Entry");
                    //   Console.WriteLine("   " + entry.Name);
                    //   Console.WriteLine("   " + entry.Path);
                    //   Console.WriteLine("   " + entry.Pack);
                    //    Console.WriteLine("   " + entry.WIP);

                    SeparateSongFolder? cachedSeparate = null;
                    if (zipCaching)
                    {
                        var cachePack = (FolderLevelPack) pack == FolderLevelPack.CustomWIPLevels ? FolderLevelPack.CachedWIPLevels : FolderLevelPack.NewPack;

                        SongFolderEntry cachedSongFolderEntry = new SongFolderEntry($"Cached {name}", Path.Combine(path, "Cache"), cachePack, imagePath, isWIP, false);
                        cachedSeparate = new SeparateSongFolder(cachedSongFolderEntry);
                    }

                    var seperate = new SeparateSongFolder(entry, cachedSeparate);
                    result.Add(seperate);
                    if (cachedSeparate != null)
                    {
                        result.Add(cachedSeparate);
                    }
                }
            }
            catch
            {
                Logging.Logger.Warn("Error reading folders.xml! Make sure the file is properly formatted.");
            }

            return result;
        }
    }

    public class ModSeparateSongFolder : SeparateSongFolder
    {
        public bool AlwaysShow { get; set; } = true;

        public ModSeparateSongFolder(SongFolderEntry folderEntry) : base(folderEntry)
        {
        }

        public ModSeparateSongFolder(SongFolderEntry folderEntry, UnityEngine.Sprite image) : base(folderEntry, image)
        {
        }
    }
}