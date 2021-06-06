using SongCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LogSeverity = IPA.Logging.Logger.Level;

namespace SongCore
{
    public class Converter
    {
        internal static int ConcurrentProcesses = 4;
        internal static int ActiveProcesses = 0;
        internal static int ConvertedCount = 0;
        internal static bool doneConverting = false;
        public static Stack<string> ToConvert = new Stack<string>();
        public static string oldFolderPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
        public static void PrepareExistingLibrary()
        {
            Logging.Log("Attempting to Convert Existing Library");
            if (!Directory.Exists(oldFolderPath))
            {
                Logging.Log("No Existing Library to Convert", LogSeverity.Notice);
                return;
            }
            Utils.GrantAccess(oldFolderPath);
            Loader.Instance._progressBar.ShowMessage("Converting Existing Song Library");
            var oldFolders = Directory.GetDirectories(oldFolderPath).ToList();
            float i = 0;
            foreach (var folder in oldFolders)
            {
                i++;
                if (Directory.Exists(folder))
                {
                    var results = Directory.GetFiles(folder, "info.json", SearchOption.AllDirectories);
                    foreach (var result in results)
                    {
                        var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                        if (!Directory.Exists(songPath))
                        {
                            continue;
                        }

                        if (Directory.GetFiles(songPath, "info.dat").Count() > 0)
                        {
                            continue;
                        }

                        string newPath = songPath;
                        //If song is in a subfolder, move it to CustomSongs and correct the name
                        var parent = Directory.GetParent(songPath);
                        if (parent.Name != "CustomSongs")
                        {
                            try
                            {
                                //       Logging.Log("SubFolder Song Found: " + songPath, LogSeverity.Notice);
                                //       Logging.Log("Moving Subfolder to CustomSongs", LogSeverity.Notice);
                                newPath = Path.Combine(oldFolderPath, parent.Name + " " + new DirectoryInfo(songPath).Name);
                                if (Directory.Exists(newPath))
                                {
                                    var pathNum = 1;
                                    while (Directory.Exists(newPath + $" ({pathNum})"))
                                    {
                                        ++pathNum;
                                    }

                                    newPath = newPath + $" ({pathNum})";
                                }
                                Directory.Move(songPath, newPath);
                                if (Utils.IsDirectoryEmpty(parent.FullName))
                                {
                                    //             Logging.Log("Old parent folder empty, Deleting empty folder.");
                                    Directory.Delete(parent.FullName);
                                }

                            }
                            catch (Exception ex)
                            {
                                Logging.Log($"Error attempting to correct Subfolder {songPath}: \n {ex}", LogSeverity.Error);
                            }


                        }
                        ToConvert.Push(newPath);

                    }
                }

            }
            if (File.Exists(oldFolderPath + "/../songe-converter.exe"))
            {
                Loader.Instance.StartCoroutine(ConvertSongs());
            }
            else
            {
                Logging.Log("Missing Songe converter, not converting", LogSeverity.Notice);
                Loader.Instance.RefreshSongs();
            }

        }


        internal static IEnumerator ConvertSongs()
        {
            var totalSongs = ToConvert.Count;
            Loader.Instance._progressBar.ShowMessage($"Converting {totalSongs} Existing Songs. Please Wait...");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + "songe-converter.exe" + " -k -a " + '"' + oldFolderPath + '"';
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;
            process.Start();
            yield return new WaitUntil((delegate { return doneConverting; }));
            /*
            while (ToConvert.Count > 0)
            {
                while (ActiveProcesses < ConcurrentProcesses)
                {
                    ActiveProcesses++;
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    if (ToConvert.Count == 0) break;
                    string newPath = ToConvert.Pop();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C " + "songe-converter.exe" + " -k " + '"' + newPath + '"';
                    process.StartInfo = startInfo;
                    process.EnableRaisingEvents = true;
                    process.Exited += Process_Exited;
                    process.Start();
                }
                yield return new WaitUntil((delegate { return ActiveProcesses < ConcurrentProcesses; }));
                //        if (ConvertedCount % 10 == 0)
                //        {
                //            Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
                //        }
                //        else if(ToConvert.Count <= 10)
                Loader.Instance._progressBar.ShowMessage($"Converting {ToConvert.Count} Existing Songs");
            }
            */
            Logging.Log($"Converted {totalSongs} songs.");
            FinishConversion();
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
            //       Logging.Log("Ended");
            //      ActiveProcesses--;
            //      ConvertedCount++;
            doneConverting = true;
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
                    Directory.Move(CustomLevelPathHelper.customLevelsDirectoryPath, CustomLevelPathHelper.customLevelsDirectoryPath + DateTime.Now.ToFileTime().ToString());

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
