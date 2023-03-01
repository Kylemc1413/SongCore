using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using IPA;
using SongCore.UI;
using SongCore.Utilities;
using IPA.Utilities;
using System;
using System.IO;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace SongCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private static PluginMetadata _metadata;
        private static Harmony? _harmony;

        internal static SConfiguration Configuration { get; private set; }

        public static Action<bool, string, string, IPreviewBeatmapLevel>? CustomSongPlatformSelectionDidChange;

        public static string standardCharacteristicName = "Standard";
        public static string oneSaberCharacteristicName = "OneSaber";
        public static string noArrowsCharacteristicName = "NoArrows";

        [Init]
        public void Init(IPALogger pluginLogger, PluginMetadata metadata)
        {
            // Workaround for creating BSIPA config in Userdata subdir
            Directory.CreateDirectory(Path.Combine(UnityGame.UserDataPath, nameof(SongCore)));
            Configuration = Config.GetConfigFor(nameof(SongCore) + Path.DirectorySeparatorChar + nameof(SongCore)).Generated<SConfiguration>();

            Logging.Logger = pluginLogger;
            _metadata = metadata;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            // TODO: Remove this migration path at some point
            var songCoreIniPath = Path.Combine(UnityGame.UserDataPath, nameof(SongCore), "SongCore.ini");
            if (File.Exists(songCoreIniPath))
            {
                var modPrefs = new BS_Utils.Utilities.Config("SongCore/SongCore");

                Configuration.CustomSongPlatforms = modPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
                Configuration.DisplayDiffLabels = modPrefs.GetBool("SongCore", "displayDiffLabels", true, true);
                Configuration.ForceLongPreviews = modPrefs.GetBool("SongCore", "forceLongPreviews", false, true);

                //Delete Old Config
                try
                {
                    File.Delete(songCoreIniPath);
                }
                catch
                {
                    Logging.Logger.Warn("Failed to delete old config file!");
                }
            }


            BSMLSettings.instance.AddSettingsMenu("SongCore", "SongCore.UI.settings.bsml", new SCSettingsController());
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            _harmony = Harmony.CreateAndPatchAll(_metadata.Assembly, "com.kyle1413.BeatSaber.SongCore");

            BasicUI.GetIcons();
            BS_Utils.Utilities.BSEvents.levelSelected += BSEvents_levelSelected;
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
                using var foldersXmlResourceStream = _metadata.Assembly.GetManifestResourceStream("SongCore.Data.folders.xml");
                using var fileStream = File.OpenWrite(foldersXmlFilePath);
                foldersXmlResourceStream!.CopyTo(fileStream);
            }

            Loader.SeperateSongFolders.InsertRange(0, Data.SeperateSongFolder.ReadSeperateFoldersFromFile(foldersXmlFilePath));
        }

        private void BSEvents_menuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
        {
            Loader.OnLoad();
            RequirementsUI.instance.Setup();
        }

        private void BSEvents_levelSelected(LevelCollectionViewController arg1, IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel));

                if (songData == null)
                {
                    return;
                }

                if (Configuration.CustomSongPlatforms && !string.IsNullOrWhiteSpace(songData._customEnvironmentName))
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
            Object.Destroy(GameObject.Find("SongCore Color Setter"));

            if (nextScene.name == "MenuViewControllers")
            {
                BS_Utils.Gameplay.Gamemode.Init();
            }
        }

        [OnExit]
        public void OnApplicationExit()
        {
            // Suppress BSIPA warning about missing [OnExit] annotated method
        }
    }
}