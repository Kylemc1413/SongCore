using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPA;
using Harmony;
using IPALogger = IPA.Logging.Logger;
using SongCore.Utilities;
using BSEvents = CustomUI.Utilities.BSEvents;
using Newtonsoft.Json;
using UnityEngine.Networking;
namespace SongCore
{
    public class Plugin : IBeatSaberPlugin
    {
        public static string standardCharacteristicName = "LEVEL_STANDARD";
        public static string oneSaberCharacteristicName = "LEVEL_ONE_SABER";
        public static string noArrowsCharacteristicName = "LEVEL_NO_ARROWS";
        internal static HarmonyInstance harmony;
        internal static bool ColorsInstalled = false;
        internal static bool PlatformsInstalled = false;
        internal static bool customSongColors;
        internal static bool customSongPlatforms;
        internal static int _currentPlatform = -1;


        
        public void OnApplicationStart()
        {
            ColorsInstalled = Utils.IsModInstalled("Custom Colors") || Utils.IsModInstalled("Chroma");
            PlatformsInstalled = Utils.IsModInstalled("Custom Platforms"); 
            harmony = HarmonyInstance.Create("com.kyle1413.BeatSaber.SongCore");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            if (!File.Exists(Collections.dataPath)) File.Create(Collections.dataPath);
       //     Collections.LoadExtraSongData();
            UI.BasicUI.GetIcons();
            CustomUI.Utilities.BSEvents.levelSelected += BSEvents_levelSelected;
            CustomUI.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
            Collections.LoadExtraSongData();
            Collections.RegisterCustomCharacteristic(UI.BasicUI.MissingCharIcon, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic");
            Collections.RegisterCustomCharacteristic(UI.BasicUI.LightshowIcon, "Lightshow", "Lightshow", "Lightshow", "Lightshow");
            Collections.RegisterCustomCharacteristic(UI.BasicUI.ExtraDiffsIcon, "Lawless", "Lawless - These difficulties don't follow conventional standards, and should not necessarily be expected to reflect their given names.", "Lawless", "Lawless");



        }

        private void BSEvents_gameSceneLoaded()
        {
            SharedCoroutineStarter.instance.StartCoroutine(DelayedNoteJumpMovementSpeedFix());
        }

        private void BSEvents_levelSelected(LevelPackLevelsViewController arg1, IBeatmapLevel level)
        {
            Data.ExtraSongData songData = Collections.RetrieveExtraSongData(level.levelID);
            if (songData == null)
            {
      //          Logging.Log("Null song Data");
                return;
            }
      //      Logging.Log($"Platforms Installed: {PlatformsInstalled}. Platforms enabled: {customSongPlatforms}");
            if (PlatformsInstalled && customSongPlatforms)
            {
                if (!string.IsNullOrWhiteSpace(songData.customEnvironmentName))
                {
                    if (findCustomEnvironment(songData.customEnvironmentName) == -1)
                    {
                        Console.WriteLine("CustomPlatform not found: " + songData.customEnvironmentName);
                        if (!string.IsNullOrWhiteSpace(songData.customEnvironmentHash))
                        {
                            Console.WriteLine("Downloading with hash: " + songData.customEnvironmentHash);
                            SharedCoroutineStarter.instance.StartCoroutine(downloadCustomPlatform(songData.customEnvironmentHash, songData.customEnvironmentName));
                        }
                    }
                }
            }
        }

        public void Init(object thisIsNull, IPALogger pluginLogger)
        {

            Utilities.Logging.logger = pluginLogger;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
            {
                UI.BasicUI.CreateUI();
                if (UI.BasicUI.reqDialog == null)
                    UI.BasicUI.InitRequirementsMenu();
            }

        }

        public void OnSceneUnloaded(Scene scene)
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            customSongColors = UI.BasicUI.ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            customSongPlatforms = UI.BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            GameObject.Destroy(GameObject.Find("SongCore Color Setter"));
            if (nextScene.name == "MenuCore")
            {
                BS_Utils.Gameplay.Gamemode.Init();
                if (PlatformsInstalled)
                    CheckForPreviousPlatform();

            }

            if (nextScene.name == "GameCore")
            {
                GameplayCoreSceneSetupData data = BS_Utils.Plugin.LevelData?.GameplayCoreSceneSetupData;
                Data.ExtraSongData.DifficultyData songData = Collections.RetrieveDifficultyData(data.difficultyBeatmap);
                if (songData != null)
                {
                    if (PlatformsInstalled)
                        CheckCustomSongEnvironment(data.difficultyBeatmap);

                    if (songData.colorLeft != null && songData.colorRight != null)
                    {
                        if (customSongColors)
                            SetSongColors(songData.colorLeft, songData.colorRight);
                    }
                }

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

                AdjustNJS(BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpMovementSpeed, beatmapObjectSpawnController);
                
            }
        }

        public static void AdjustNJS(float njs, BeatmapObjectSpawnController _spawnController)
        {

            float halfJumpDur = 4f;
            float maxHalfJump = _spawnController.GetPrivateField<float>("_maxHalfJumpDistance");
            float noteJumpStartBeatOffset = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpStartBeatOffset;
            float moveSpeed = _spawnController.GetPrivateField<float>("_moveSpeed");
            float moveDir = _spawnController.GetPrivateField<float>("_moveDurationInBeats");
            float jumpDis;
            float spawnAheadTime;
            float moveDis;
            float bpm = _spawnController.GetPrivateField<float>("_beatsPerMinute");
            float num = 60f / bpm;
            moveDis = moveSpeed * num * moveDir;
            while (njs * num * halfJumpDur > maxHalfJump)
            {
                halfJumpDur /= 2f;
            }
            halfJumpDur += noteJumpStartBeatOffset;
            if (halfJumpDur < 1f) halfJumpDur = 1f;
            //        halfJumpDur = spawnController.GetPrivateField<float>("_halfJumpDurationInBeats");
            jumpDis = njs * num * halfJumpDur * 2f;
            spawnAheadTime = moveDis / moveSpeed + jumpDis * 0.5f / njs;
            _spawnController.SetPrivateField("_halfJumpDurationInBeats", halfJumpDur);
            _spawnController.SetPrivateField("_spawnAheadTime", spawnAheadTime);
            _spawnController.SetPrivateField("_jumpDistance", jumpDis);
            _spawnController.SetPrivateField("_noteJumpMovementSpeed", njs);
            _spawnController.SetPrivateField("_moveDistance", moveDis);


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

        private void SetSongColors(Data.ExtraSongData.MapColor left, Data.ExtraSongData.MapColor right)
        {
            Color colorLeft = new Color(left.r, left.g, left.b);
            Color colorRight = new Color(right.r, right.g, right.b);
            GameObject colorSetterObj = null;
            EnvironmentColorsSetter colorSetter;
            if (BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level.environmentSceneInfo.sceneName.Contains("KDA"))
            {
                //     Console.WriteLine("KDA");
                colorSetter = Resources.FindObjectsOfTypeAll<EnvironmentColorsSetter>().FirstOrDefault();
            }
            else
            {
                colorSetterObj = new GameObject("SongCore Color Setter");

                colorSetterObj.SetActive(false);
                colorSetter = colorSetterObj.AddComponent<EnvironmentColorsSetter>();
            }

            var scriptableColors = Resources.FindObjectsOfTypeAll<SimpleColorSO>();
            SimpleColorSO[] A = new SimpleColorSO[2];
            SimpleColorSO[] B = new SimpleColorSO[2];
            foreach (var color in scriptableColors)
            {
                //     Console.WriteLine("Color: " + color.name);
                int i = 0;
                if (color.name == "BaseNoteColor1")
                {
                    B[0] = color;
                    i++;
                }
                else if (color.name == "BaseNoteColor0")
                {
                    A[0] = color;
                    i++;
                }
                else if (color.name == "BaseColor0")
                {
                    A[1] = color;
                    i++;
                }
                else if (color.name == "BaseColor1")
                {
                    B[1] = color;
                    i++;
                }
            }
            colorSetter.SetPrivateField("_colorsA", A);
            colorSetter.SetPrivateField("_colorsB", B);
            colorSetter.SetPrivateField("_colorManager", Resources.FindObjectsOfTypeAll<ColorManager>().First());
            colorSetter.SetPrivateField("_overrideColorA", colorRight);
            colorSetter.SetPrivateField("_overrideColorB", colorLeft);
            //    Console.WriteLine("Turning on");
            if (colorSetterObj != null)
                colorSetterObj.SetActive(true);

            colorSetter.Awake();


        }

        private void CheckCustomSongEnvironment(IDifficultyBeatmap song)
        {
            Data.ExtraSongData songData = Collections.RetrieveExtraSongData(song.level.levelID);
            if (songData == null) return;
            if (string.IsNullOrWhiteSpace(songData.customEnvironmentName)) return;
            int _customPlatform = customEnvironment(songData.customEnvironmentName);
            if (_customPlatform != -1)
            {
                _currentPlatform = CustomFloorPlugin.PlatformManager.Instance.currentPlatformIndex;
                if (customSongPlatforms && _customPlatform != _currentPlatform)
                {
                    CustomFloorPlugin.PlatformManager.Instance.ChangeToPlatform(_customPlatform, false);
                }
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

            CustomFloorPlugin.CustomPlatform[] _customPlatformsList = CustomFloorPlugin.PlatformManager.Instance.GetPlatforms();
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
        private void CheckForPreviousPlatform()
        {
            if (_currentPlatform != -1)
            {
                CustomFloorPlugin.PlatformManager.Instance.ChangeToPlatform(_currentPlatform);
            }
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
                    CustomFloorPlugin.PlatformManager.Instance.AddPlatform(customPlatformsFolderPath + ".plat");
                }
            }
        }
    }
}


