using System;
using System.IO;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using IPA.Utilities;
using SiraUtil.Zenject;
using SongCore.Installers;
using SongCore.Patches;
using SongCore.UI;
using SongCore.Utilities;

namespace SongCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private readonly PluginMetadata _metadata;
        private readonly Harmony _harmony;

        public static Action<bool, string, string, BeatmapLevel>? CustomSongPlatformSelectionDidChange;

        [Init]
        public Plugin(Logger logger, PluginMetadata metadata, Zenjector zenjector)
        {
            // Workaround for creating BSIPA config in Userdata subdir
            Directory.CreateDirectory(Path.Combine(UnityGame.UserDataPath, nameof(SongCore)));

            Logging.Logger = logger;
            _metadata = metadata;
            _harmony = new Harmony("com.kyle1413.BeatSaber.SongCore");

            zenjector.UseLogger(logger);
            zenjector.Install<AppInstaller>(Location.App, Config.GetConfigFor(nameof(SongCore) + Path.DirectorySeparatorChar + nameof(SongCore)).Generated<PluginConfig>());
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<GameInstaller>(Location.StandardPlayer);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            if (typeof(Harmony).Assembly.GetName().Version.Minor < 12)
            {
                _harmony.Patch(HarmonyTranspilersFixPatch.TargetMethod(), null, null, new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyTranspilersFixPatch), nameof(HarmonyTranspilersFixPatch.Transpiler))));
            }
            _harmony.PatchAll(_metadata.Assembly);

            BasicUI.GetIcons();

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

            Loader.SeparateSongFolders.InsertRange(0, Data.SeparateSongFolder.ReadSeparateFoldersFromFile(foldersXmlFilePath));
        }

        [OnExit]
        public void OnApplicationExit()
        {
            // Suppress BSIPA warning about missing [OnExit] annotated method
        }
    }
}