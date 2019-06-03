using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongCore.Utilities;
using SongCore.OverrideClasses;
using LogSeverity = IPA.Logging.Logger.Level;

namespace SongCore
{
    public class Converter
    {
        public static string oldFolderPath = Environment.CurrentDirectory + "/CustomSongs";
        public static void ConvertExistingLibrary()
        {
            Logging.Log("Attempting to Convert Existing Library");
            if (!Directory.Exists(oldFolderPath))
            {
                Logging.Log("No Existing Library to Convert", LogSeverity.Notice);
                return;
            }
            var oldFolders = Directory.GetDirectories(oldFolderPath);
            float i = 0;
            foreach(var folder in oldFolders)
            {
                i++;
                var results = Directory.GetFiles(folder, "info.json", SearchOption.AllDirectories);
                foreach(var result in results)
                {
                    var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));

                    //If song is in a subfolder, move it to CustomSongs and correct the name
                    var parent = Directory.GetParent(songPath);
                    if (parent.Name != "CustomSongs")
                    {
                        Logging.Log("SubFolder Song Found: " + songPath, LogSeverity.Notice);
                        Logging.Log("Moving Subfolder to CustomSongs", LogSeverity.Notice);
                        string newPath = oldFolderPath + "/" + parent.Name + " " + new DirectoryInfo(songPath).Name;
                        if(Directory.Exists(newPath))
                        {
                            int pathNum = 1;
                            while (Directory.Exists(newPath + $" ({pathNum})")) ++pathNum;
                            newPath = newPath + $" ({pathNum})";
                        }
                        Directory.Move(songPath, newPath);
                        if (Utils.IsDirectoryEmpty(parent.FullName))
                        {
                            Logging.Log("Old parent folder empty, Deleting empty folder.");
                            Directory.Delete(parent.FullName);
                        }
                    }

                }
            }

        }


    }
}
