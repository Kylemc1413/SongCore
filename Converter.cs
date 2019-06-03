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
        internal static int ConcurrentProcesses = 4;
        internal static int ActiveProcesses = 0;
        internal static int ConvertedCount = 0;
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
            if(File.Exists(oldFolderPath + "/../songe-converter.exe"))
                Loader.Instance.StartCoroutine(ConvertSongs());
            else
            {
                Logging.Log("Missing Songe converter, not converting", LogSeverity.Notice);
                Loader.Instance.RefreshSongs();
            }

        }


        internal static IEnumerator ConvertSongs()
        {
    //        int totalSongs = ToConvert.Count;
            Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
            while (ToConvert.Count > 0)
            {
                while(ActiveProcesses < ConcurrentProcesses)
                {
                    ActiveProcesses++;
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    if (ToConvert.Count == 0) break;
                    string newPath = ToConvert.Pop();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C " + "songe-converter.exe" + " -k " + '"' + newPath + '"';
                    process.StartInfo = startInfo;
                    process.EnableRaisingEvents = true;
                    process.Exited += Process_Exited;
                    process.Start();
                }
                yield return new WaitUntil( (delegate { return ActiveProcesses < ConcurrentProcesses; }));
        //        if (ConvertedCount % 10 == 0)
        //        {
        //            Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
        //        }
        //        else if(ToConvert.Count <= 10)
                    Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
            }
            Logging.Log($"Converted {ConvertedCount} songs.");
            FinishConversion();
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
     //       Logging.Log("Ended");
            ActiveProcesses--;
            ConvertedCount++;
        }

        internal static void FinishConversion()
        {
            if (Directory.Exists(oldFolderPath))
            {
                //    Logging.Log(CustomLevelPathHelper.customLevelsDirectoryPath);
                //    Logging.Log((CustomLevelPathHelper.customLevelsDirectoryPath + System.DateTime.Now.ToFileTime().ToString()));
                //    Logging.Log(oldFolderPath);
                Logging.Log("Moving CustomSongs folder to new Location");
                if (Directory.Exists(CustomLevelPathHelper.customLevelsDirectoryPath))
                {
                    Utils.GrantAccess(CustomLevelPathHelper.customLevelsDirectoryPath);
                    Directory.Move(CustomLevelPathHelper.customLevelsDirectoryPath, CustomLevelPathHelper.customLevelsDirectoryPath + System.DateTime.Now.ToFileTime().ToString());

                }
                Utils.GrantAccess(oldFolderPath);
                Directory.Move(oldFolderPath, CustomLevelPathHelper.customLevelsDirectoryPath);
                //    Directory.Delete(oldFolderPath);
            }
            Logging.Log("Conversion Finished. Loading songs");
            Loader.Instance.RefreshSongs();
        }
    }
}
