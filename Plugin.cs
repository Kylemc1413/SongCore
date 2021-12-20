using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using SongCore.UI;
using SongCore.Utilities;
using IPA.Utilities;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace SongCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private static Harmony? _harmony;

        internal static bool CustomSongColors = SCSettings.instance.Colors;
        internal static bool CustomSongPlatforms = SCSettings.instance.Platforms;
        internal static bool DisplayDiffLabels = SCSettings.instance.DiffLabels;
        internal static bool ForceLongPreviews = SCSettings.instance.LongPreviews;

        public static Action<bool, string, string, IPreviewBeatmapLevel>? CustomSongPlatformSelectionDidChange;

        public static string standardCharacteristicName = "Standard";
        public static string oneSaberCharacteristicName = "OneSaber";
        public static string noArrowsCharacteristicName = "NoArrows";

        [Init]
        public void Init(IPALogger pluginLogger)
        {
            Logging.Logger = pluginLogger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            BSMLSettings.instance.AddSettingsMenu("SongCore", "SongCore.UI.settings.bsml", SCSettings.instance);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            //Delete Old Config
            try
            {
                var songCoreIniPath = Path.Combine(UnityGame.UserDataPath, "SongCore.ini");
                if (File.Exists(songCoreIniPath))
                {
                    File.Delete(songCoreIniPath);
                }
            }
            catch
            {
                Logging.Logger.Warn("Failed to delete old config file!");
            }

            _harmony = new Harmony("com.kyle1413.BeatSaber.SongCore");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            BasicUI.GetIcons();
            BS_Utils.Utilities.BSEvents.levelSelected += BSEvents_levelSelected;
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += BSEvents_menuSceneLoadedFresh;

            if (!File.Exists(Collections.DataPath))
            {
                File.Create(Collections.DataPath);
            }
            else
            {
                Collections.LoadExtraSongData();
            }

            Collections.RegisterCustomCharacteristic(BasicUI.MissingCharIcon!, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic", false, false, 1000);
            Collections.RegisterCustomCharacteristic(BasicUI.LightshowIcon!, "Lightshow", "Lightshow", "Lightshow", "Lightshow", false, false, 100);
            Collections.RegisterCustomCharacteristic(BasicUI.ExtraDiffsIcon!, "Lawless", "Lawless - Anything Goes", "Lawless", "Lawless", false, false, 101);

            var foldersXmlFilePath = Path.Combine(UnityGame.UserDataPath, nameof(SongCore), "folders.xml");
            if (!File.Exists(foldersXmlFilePath))
            {
                File.WriteAllBytes(foldersXmlFilePath, Utilities.Utils.GetResource(Assembly.GetExecutingAssembly(), "SongCore.Data.folders.xml"));
            }

            Loader.SeperateSongFolders.InsertRange(0, Data.SeperateSongFolder.ReadSeperateFoldersFromFile(foldersXmlFilePath));
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Collections.SaveExtraSongData();
        }

        private void BSEvents_menuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
        {
            Loader.OnLoad();
            RequirementsUI.instance.Setup();
        }

        private void BSEvents_gameSceneLoaded()
        {
            if (!BS_Utils.Plugin.LevelData.IsSet)
            {
                return;
            }

            SharedCoroutineStarter.instance.StartCoroutine(DelayedNoteJumpMovementSpeedFix());
        }

        private void BSEvents_levelSelected(LevelCollectionViewController arg1, IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);

                if (songData == null)
                {
                    return;
                }

                if (CustomSongPlatforms && !string.IsNullOrWhiteSpace(songData._customEnvironmentName))
                {
                    Logging.Logger.Debug("Custom song with platform selected");
                    CustomSongPlatformSelectionDidChange?.Invoke(true, songData._customEnvironmentName, songData._customEnvironmentHash, customLevel);
                }
                else
                {
                    CustomSongPlatformSelectionDidChange?.Invoke(false, songData._customEnvironmentName, songData._customEnvironmentHash, customLevel);
                }
            }
        }

        private void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            CustomSongColors = BasicUI.ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            CustomSongPlatforms = BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            DisplayDiffLabels = BasicUI.ModPrefs.GetBool("SongCore", "displayDiffLabels", true, true);
            Object.Destroy(GameObject.Find("SongCore Color Setter"));

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
                var beatmapObjectSpawnController = Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>().LastOrDefault();

                SetNJS(beatmapObjectSpawnController);
            }
        }

        public static void SetNJS(BeatmapObjectSpawnController spawnController)
        {
            BeatmapObjectSpawnMovementData spawnMovementData = spawnController.GetField<BeatmapObjectSpawnMovementData, BeatmapObjectSpawnController>("_beatmapObjectSpawnMovementData");

            var bpm = spawnController.GetField<VariableBpmProcessor, BeatmapObjectSpawnController>("_variableBPMProcessor").currentBpm;

            spawnMovementData.SetField("_startNoteJumpMovementSpeed", BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpMovementSpeed);
            spawnMovementData.SetField("_noteJumpStartBeatOffset", BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.noteJumpStartBeatOffset);

            spawnMovementData.Update(bpm, spawnController.GetField<float, BeatmapObjectSpawnController>("_jumpOffsetY"));
        }
    }
}