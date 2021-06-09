using SongCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SongCore
{
    public class Converter
    {
        internal static int ConcurrentProcesses = 4;
        internal static int ActiveProcesses = 0;
        internal static int ConvertedCount = 0;
        internal static bool doneConverting = false;

        public static readonly Stack<string> ToConvert = new Stack<string>();
        public static readonly string oldFolderPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");

        public static void PrepareExistingLibrary()
        {
            Logging.Logger.Info("Attempting to Convert Existing Library");
            if (!Directory.Exists(oldFolderPath))
            {
                Logging.Logger.Notice("No Existing Library to Convert");
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

                        if (Directory.EnumerateFiles(songPath, "info.dat").Any())
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
                                newPath = Path.Combine(oldFolderPath, $"{parent.Name} {new DirectoryInfo(songPath).Name}");
                                if (Directory.Exists(newPath))
                                {
                                    var pathNum = 1;
                                    while (Directory.Exists($"{newPath} ({pathNum})"))
                                    {
                                        ++pathNum;
                                    }

                                    newPath = $"{newPath} ({pathNum})";
                                }

                                Directory.Move(songPath, newPath);
                                if (Utils.IsDirectoryEmpty(parent.FullName))
                                {
                                    Directory.Delete(parent.FullName);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Logger.Error($"Error attempting to correct Subfolder {songPath}:");
                                Logging.Logger.Error(ex);
                            }
                        }

                        ToConvert.Push(newPath);
                    }
                }
            }

            if (File.Exists(Path.Combine(oldFolderPath, "..", "songe-converter.exe")))
            {
                Loader.Instance.StartCoroutine(ConvertSongs());
            }
            else
            {
                Logging.Logger.Notice("Missing Songe converter, not converting");
                Loader.Instance.RefreshSongs();
            }
        }


        internal static IEnumerator ConvertSongs()
        {
            var totalSongs = ToConvert.Count;
            Loader.Instance._progressBar.ShowMessage($"Converting {totalSongs} Existing Songs. Please Wait...");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments = $"/C songe-converter.exe -k -a \"{oldFolderPath}\"",
            };
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;
            process.Start();
            yield return new WaitUntil((() => doneConverting));
            Logging.Logger.Info($"Converted {totalSongs} songs.");
            FinishConversion();
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
            doneConverting = true;
        }

        internal static void FinishConversion()
        {
            if (Directory.Exists(oldFolderPath))
            {
                Logging.Logger.Info("Moving CustomSongs folder to new Location");
                if (Directory.Exists(CustomLevelPathHelper.customLevelsDirectoryPath))
                {
                    Utils.GrantAccess(CustomLevelPathHelper.customLevelsDirectoryPath);
                    Directory.Move(CustomLevelPathHelper.customLevelsDirectoryPath, CustomLevelPathHelper.customLevelsDirectoryPath + DateTime.Now.ToFileTime().ToString());
                }

                Utils.GrantAccess(oldFolderPath);
                Directory.Move(oldFolderPath, CustomLevelPathHelper.customLevelsDirectoryPath);
            }

            Logging.Logger.Info("Conversion Finished. Loading songs");
            Loader.Instance.RefreshSongs();
        }
    }
}