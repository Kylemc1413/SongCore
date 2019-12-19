using SongCore.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace SongCore.Data
{
    public enum FolderLevelPack { CustomLevels, CustomWIPLevels, NewPack };
    [Serializable]
    public class SongFolderEntry
    {
        public string Name;
        public string Path;
        public FolderLevelPack Pack;
        public string ImagePath;
        public bool WIP;

        public SongFolderEntry(string name, string path, FolderLevelPack pack, string imagePath = "", bool wip = false)
        {
            Name = name;
            Path = path;
            Pack = pack;
            ImagePath = imagePath;
            WIP = wip;
        }
    }
    public class SeperateSongFolder
    {
        public SongFolderEntry SongFolderEntry { get; private set; }
        public Dictionary<string, CustomPreviewBeatmapLevel> Levels = new Dictionary<string, CustomPreviewBeatmapLevel>();
        public SongCoreCustomLevelCollection LevelCollection { get; private set; } = null;
        public SongCoreCustomBeatmapLevelPack LevelPack { get; private set; } = null;

        public SeperateSongFolder(SongFolderEntry folderEntry)
        {
            SongFolderEntry = folderEntry;
            if (folderEntry.Pack == FolderLevelPack.NewPack)
            {
                LevelCollection = new SongCoreCustomLevelCollection(Levels.Values.ToArray());
                UnityEngine.Sprite image = UI.BasicUI.FolderIcon;
                if (!string.IsNullOrEmpty(folderEntry.ImagePath))
                {
                    try
                    {
                        var packImage = SongCore.Utilities.Utils.LoadSpriteFromFile(folderEntry.ImagePath);
                        if (packImage != null)
                            image = packImage;
                    }
                    catch
                    {
                        SongCore.Utilities.Logging.Log($"Failed to Load Image For Seperate Folder \"{folderEntry.Name}\"");
                    }
                }

                LevelPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + folderEntry.Name, folderEntry.Name, image, LevelCollection);
            }
        }
        public SeperateSongFolder(SongFolderEntry folderEntry, UnityEngine.Sprite Image)
        {
            SongFolderEntry = folderEntry;
            if (folderEntry.Pack == FolderLevelPack.NewPack)
            {
                LevelCollection = new SongCoreCustomLevelCollection(Levels.Values.ToArray());

                LevelPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + folderEntry.Name, folderEntry.Name, Image, LevelCollection);
            }
        }
        public static List<SeperateSongFolder> ReadSeperateFoldersFromFile(string filePath)
        {
            List<SeperateSongFolder> result = new List<SeperateSongFolder>();
            try
            {
                XDocument file = XDocument.Load(filePath);
                foreach (var item in file.Root.Elements())
                {
                    //           Console.WriteLine("Element Name: " + item.Name);
                    string name = item.Element("Name").Value;
                    if (name == "Example") continue;
                    string path = item.Element("Path").Value;
                    int pack = int.Parse(item.Element("Pack").Value);
                    string imagePath = "";
                    var image = item.Element("ImagePath");
                    if (image != null)
                    {
                        imagePath = image.Value;
                    }

                    bool isWIP = false;
                    var wip = item.Element("WIP");
                    if (wip != null)
                    {
                        isWIP = bool.Parse(wip.Value);
                    }
                    SongFolderEntry entry = new SongFolderEntry(name, path, (FolderLevelPack)pack, imagePath, isWIP);
                    //   Console.WriteLine("Entry");
                    //   Console.WriteLine("   " + entry.Name);
                    //   Console.WriteLine("   " + entry.Path);
                    //   Console.WriteLine("   " + entry.Pack);
                    //    Console.WriteLine("   " + entry.WIP);
                    result.Add(new SeperateSongFolder(entry));
                }
            }
            catch
            {
                Utilities.Logging.Log("Error Reading folders.xml! Make sure the file is properly formatted.", IPA.Logging.Logger.Level.Warning);
            }

            return result;

        }
    }
    public class ModSeperateSongFolder : SeperateSongFolder
    {
        public ModSeperateSongFolder(SongFolderEntry folderEntry) : base(folderEntry)
        {

        }

        public ModSeperateSongFolder(SongFolderEntry folderEntry, UnityEngine.Sprite Image) : base(folderEntry, Image)
        {

        }
    }
}
