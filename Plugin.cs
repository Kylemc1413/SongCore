using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using Newtonsoft.Json;
using SongCore.UI;
using SongCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace SongCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static string standardCharacteristicName = "Standard";
        public static string oneSaberCharacteristicName = "OneSaber";
        public static string noArrowsCharacteristicName = "NoArrows";
        internal static Harmony harmony;
        //     internal static bool ColorsInstalled = false;
        internal static bool PlatformsInstalled = false;
        internal static bool customSongColors;
        internal static bool customSongPlatforms;
        internal static bool displayDiffLabels;
        internal static int _currentPlatform = -1;

        [OnStart]
        public void OnApplicationStart()
        {
            BSMLSettings.instance.AddSettingsMenu("SongCore", "SongCore.UI.settings.bsml", SCSettings.instance);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;

            //Delete Old Config
            try
            {
                if (File.Exists(Environment.CurrentDirectory + "/UserData/SongCore.ini"))
                    File.Delete(Environment.CurrentDirectory + "/UserData/SongCore.ini");
            }
            catch
            {
                Logging.logger.Warn("Failed to delete old config file!");
            }

            //      ColorsInstalled = Utils.IsModInstalled("Custom Colors") || Utils.IsModInstalled("Chroma");
            PlatformsInstalled = Utils.IsModInstalled("Custom Platforms");
            harmony = new Harmony("com.kyle1413.BeatSaber.SongCore");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            //     Collections.LoadExtraSongData();
            UI.BasicUI.GetIcons();
            BS_Utils.Utilities.BSEvents.levelSelected += BSEvents_levelSelected;
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += BSEvents_menuSceneLoadedFresh;
            if (!File.Exists(Collections.dataPath))
                File.Create(Collections.dataPath);
            else
                Collections.LoadExtraSongData();
            Collections.RegisterCustomCharacteristic(UI.BasicUI.MissingCharIcon, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic", false, false, 1000);
            Collections.RegisterCustomCharacteristic(UI.BasicUI.LightshowIcon, "Lightshow", "Lightshow", "Lightshow", "Lightshow", false, false, 100);
            Collections.RegisterCustomCharacteristic(UI.BasicUI.ExtraDiffsIcon, "Lawless", "Lawless - Anything Goes", "Lawless", "Lawless", false, false, 101);

            if (!File.Exists(Environment.CurrentDirectory + "/UserData/SongCore/folders.xml"))
                File.WriteAllBytes(Environment.CurrentDirectory + "/UserData/SongCore/folders.xml", Utils.GetResource(Assembly.GetExecutingAssembly(), "SongCore.Data.folders.xml"));
            Loader.SeperateSongFolders.InsertRange(0, Data.SeperateSongFolder.ReadSeperateFoldersFromFile(Environment.CurrentDirectory + "/UserData/SongCore/folders.xml"));
        }

        private void BSEvents_menuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
        {
            Loader.OnLoad();
            RequirementsUI.instance.Setup();
        }

        private void BSEvents_gameSceneLoaded()
        {
            SharedCoroutineStarter.instance.StartCoroutine(DelayedNoteJumpMovementSpeedFix());
        }

        private void BSEvents_levelSelected(LevelCollectionViewController arg1, IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel)
            {
                var customLevel = level as CustomPreviewBeatmapLevel;
                //       Logging.Log((level as CustomPreviewBeatmapLevel).customLevelPath);
                Data.ExtraSongData songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                Collections.SaveExtraSongData();

                if (songData == null)
                {
                    //          Logging.Log("Null song Data");
                    return;
                }
                //      Logging.Log($"Platforms Installed: {PlatformsInstalled}. Platforms enabled: {customSongPlatforms}");
                /*
                if (PlatformsInstalled && customSongPlatforms)
                {
                    if (!string.IsNullOrWhiteSpace(songData._customEnvironmentName))
                    {
                        if (findCustomEnvironment(songData._customEnvironmentName) == -1)
                        {
                            Console.WriteLine("CustomPlatform not found: " + songData._customEnvironmentName);
                            if (!string.IsNullOrWhiteSpace(songData._customEnvironmentHash))
                            {
                                Console.WriteLine("Downloading with hash: " + songData._customEnvironmentHash);
                                SharedCoroutineStarter.instance.StartCoroutine(downloadCustomPlatform(songData._customEnvironmentHash, songData._customEnvironmentName));
                            }
                        }
                    }
                }
                */
            }

        }
        [Init]
        public void Init(object thisIsNull, IPALogger pluginLogger)
        {

            Utilities.Logging.logger = pluginLogger;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {

        }
       
        public void OnSceneUnloaded(Scene scene)
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            customSongColors = UI.BasicUI.ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            customSongPlatforms = UI.BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            displayDiffLabels = UI.BasicUI.ModPrefs.GetBool("SongCore", "displayDiffLabels", true, true);
            GameObject.Destroy(GameObject.Find("SongCore Color Setter"));
            if (nextScene.name == "MenuViewControllers")
            {
                BS_Utils.Gameplay.Gamemode.Init();

            }
        }

        private IEnumerator DelayedNoteJumpMovementSpeedFix()
        {
            yield return new WaitForSeconds(0.1f);
            //Beat Saber 0.11.1 introduced a check for if noteJumpMovementSpeed <= 0
            //This breaks songs that have a negative noteJumpMovementSpeed and previously required a patcher to get working again
            //I've added this to add support for that again, because why not.
            if (BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpMovementSpeed < 0)
            {
                var beatmapObjectSpawnController =
                    Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>().FirstOrDefault();

                SetNJS(beatmapObjectSpawnController);

            }
        }

        public static void SetNJS(BeatmapObjectSpawnController _spawnController)
        {
            BeatmapObjectSpawnMovementData spawnMovementData =
  _spawnController.GetPrivateField<BeatmapObjectSpawnMovementData>("_beatmapObjectSpawnMovementData");

            float bpm = _spawnController.GetPrivateField<VariableBpmProcessor>("_variableBPMProcessor").currentBpm;



            spawnMovementData.SetPrivateField("_startNoteJumpMovementSpeed", BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpMovementSpeed);
            spawnMovementData.SetPrivateField("_noteJumpStartBeatOffset", BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpStartBeatOffset);

            spawnMovementData.Update(bpm,
                _spawnController.GetPrivateField<float>("_jumpOffsetY"));
        }

        public void OnApplicationQuit()
        {

        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {


        }

        public void OnFixedUpdate()
        {
        }

        /*
        internal static void CheckCustomSongEnvironment(IDifficultyBeatmap song)
        {
            Data.ExtraSongData songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(song.level as CustomPreviewBeatmapLevel));
            if (songData == null) return;
            if (string.IsNullOrWhiteSpace(songData._customEnvironmentName))
            {
                _currentPlatform = -1;
                return;
            }
            try
            {
                int _customPlatform = customEnvironment(songData._customEnvironmentName);
                if (_customPlatform != -1)
                {
                    _currentPlatform = CustomFloorPlugin.PlatformManager.CurrentPlatformIndex;
                    if (customSongPlatforms && _customPlatform != _currentPlatform)
                    {
                        CustomFloorPlugin.PlatformManager.TempChangeToPlatform(_customPlatform);
                    }
                }
            }
            catch(Exception ex)
            {
                Logging.logger.Error($"Failed to Change to Platform {songData._customEnvironmentName}\n {ex}");
            }

        }

        internal static int customEnvironment(string platform)
        {
            if (!PlatformsInstalled)
                return -1;
            return findCustomEnvironment(platform);
        }
        private static int findCustomEnvironment(string name)
        {

            List<CustomFloorPlugin.CustomPlatform> _customPlatformsList = CustomFloorPlugin.PlatformManager.AllPlatforms;
            int platIndex = 0;
            foreach (CustomFloorPlugin.CustomPlatform plat in _customPlatformsList)
            {
                if (plat?.platName == name)
                    return platIndex;
                platIndex++;
            }
            Console.WriteLine(name + " not found!");

    
            return -1;
        }

        [Serializable]
        public class platformDownloadData
        {
            public string name;
            public string author;
            public string image;
            public string hash;
            public string download;
            public string date;
        }

        private IEnumerator downloadCustomPlatform(string hash, string name)
        {
            using (UnityWebRequest www = UnityWebRequest.Get("https://modelsaber.com/api/v1/platform/get.php?filter=hash:" + hash))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Console.WriteLine(www.error);
                }
                else
                {
                    var downloadData = JsonConvert.DeserializeObject<Dictionary<string, platformDownloadData>>(www.downloadHandler.text);
                    platformDownloadData data = downloadData.FirstOrDefault().Value;
                    if (data != null)
                        if (data.name == name)
                        {
                            SharedCoroutineStarter.instance.StartCoroutine(_downloadCustomPlatform(data));
                        }
                }
            }
        }

        private IEnumerator _downloadCustomPlatform(platformDownloadData downloadData)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(downloadData.download))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Console.WriteLine(www.error);
                }
                else
                {
                    string customPlatformsFolderPath = Path.Combine(Environment.CurrentDirectory, "CustomPlatforms", downloadData.name);
                    System.IO.File.WriteAllBytes(@customPlatformsFolderPath + ".plat", www.downloadHandler.data);
                    try
                    {
                    CustomFloorPlugin.PlatformManager.AddPlatform(customPlatformsFolderPath + ".plat");
                    }
                    catch(Exception ex)
                    {
                        Logging.logger.Error($"Failed to add Platform {customPlatformsFolderPath}.plat \n {ex}");
                    }
                }
            }
        }
        */
    }
}


