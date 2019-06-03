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
        public static Stack<string> ToConvert = new Stack<string>();
        public static string oldFolderPath = Environment.CurrentDirectory + "/CustomSongs";
        public static void PrepareExistingLibrary()
        {
            Logging.Log("Attempting to Convert Existing Library");
            if (!Directory.Exists(oldFolderPath))
            {
                Logging.Log("No Existing Library to Convert", LogSeverity.Notice);
                return;
            }
            Loader.Instance._progressBar.ShowMessage("Converting Existing Song Library");
            var oldFolders = Directory.GetDirectories(oldFolderPath).ToList();
            float i = 0;
            foreach(var folder in oldFolders)
            {
                i++;
                var results = Directory.GetFiles(folder, "info.json", SearchOption.AllDirectories);
                foreach(var result in results)
                {
                    var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                    if (Directory.GetFiles(songPath, "info.dat").Count() > 0)
                        continue;
                    string newPath = songPath;
                    //If song is in a subfolder, move it to CustomSongs and correct the name
                    var parent = Directory.GetParent(songPath);
                    if (parent.Name != "CustomSongs")
                    {
                 //       Logging.Log("SubFolder Song Found: " + songPath, LogSeverity.Notice);
                 //       Logging.Log("Moving Subfolder to CustomSongs", LogSeverity.Notice);
                        newPath = oldFolderPath + "/" + parent.Name + " " + new DirectoryInfo(songPath).Name;
                        if(Directory.Exists(newPath))
                        {
                            int pathNum = 1;
                            while (Directory.Exists(newPath + $" ({pathNum})")) ++pathNum;
                            newPath = newPath + $" ({pathNum})";
                        }
                        Directory.Move(songPath, newPath);
                        if (Utils.IsDirectoryEmpty(parent.FullName))
                        {
               //             Logging.Log("Old parent folder empty, Deleting empty folder.");
                            Directory.Delete(parent.FullName);
                        }
                    }
                    ToConvert.Push(newPath);

                }
            }
            Loader.Instance.StartCoroutine(ConvertSongs());

        }


        internal static IEnumerator ConvertSongs()
        {
           
            int totalSongs = ToConvert.Count;
            Loader.Instance._progressBar.ShowMessage($"Converting {totalSongs} Existing Songs");
         //   Loader.Instance._progressBar._loadingBar.enabled = true;
         //   Loader.Instance._progressBar._loadingBackg.enabled = true;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            while (ToConvert.Count > 0)
            {
                string newPath = ToConvert.Pop();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + "songe-converter.exe" + " -k " + '"' + newPath + '"';
      //          Logging.Log(startInfo.Arguments);
                process.StartInfo = startInfo;
                process.Start();
                yield return new WaitUntil((delegate { return process.HasExited; }));
                Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
            //    Loader.Instance._progressBar._loadingBar.fillAmount = (totalSongs - ToConvert.Count) / totalSongs;
            }
            FinishConversion();
        }
        internal static void FinishConversion()
        {
            if (Directory.Exists(oldFolderPath))
            {
                //    Logging.Log(CustomLevelPathHelper.customLevelsDirectoryPath);
                //    Logging.Log((CustomLevelPathHelper.customLevelsDirectoryPath + System.DateTime.Now.ToFileTime().ToString()));
                //    Logging.Log(oldFolderPath);
                Logging.Log("Moving CustomSongs folder to new Location");
                   Directory.Move(CustomLevelPathHelper.customLevelsDirectoryPath, CustomLevelPathHelper.customLevelsDirectoryPath + System.DateTime.Now.ToFileTime().ToString());
                   Directory.Move(oldFolderPath, CustomLevelPathHelper.customLevelsDirectoryPath);
                //    Directory.Delete(oldFolderPath);
            }
            Logging.Log("Conversion Finished. Loading songs");
            Loader.Instance.RefreshSongs();
        }
    }
}
