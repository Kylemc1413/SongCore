using HarmonyLib;
using IPA;
using SongCore.UI;
using SongCore.Utilities;
using IPA.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using SongCore.HarmonyPatches;
using SiraUtil.Zenject;
using SongCore.Installers;
using IPALogger = IPA.Logging.Logger;

namespace SongCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private readonly PluginMetadata _metadata;
        private readonly Harmony _harmony;

        internal static SConfiguration Configuration { get; private set; }

        public static Action<bool, string, string, BeatmapLevel>? CustomSongPlatformSelectionDidChange;

        public static string standardCharacteristicName = "Standard";
        public static string oneSaberCharacteristicName = "OneSaber";
        public static string noArrowsCharacteristicName = "NoArrows";

        [Init]
        public Plugin(IPALogger pluginLogger, PluginMetadata metadata, Zenjector zenjector)
        {
            // Workaround for creating BSIPA config in Userdata subdir
            Directory.CreateDirectory(Path.Combine(UnityGame.UserDataPath, nameof(SongCore)));
            Configuration = Config.GetConfigFor(nameof(SongCore) + Path.DirectorySeparatorChar + nameof(SongCore)).Generated<SConfiguration>();

            Logging.Logger = pluginLogger;
            _metadata = metadata;
            _harmony = new Harmony("com.kyle1413.BeatSaber.SongCore");

            zenjector.UseLogger(pluginLogger);
            zenjector.Install<AppInstaller>(Location.App);
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<GameInstaller>(Location.StandardPlayer);
        }

        [OnStart]
        public async Task OnApplicationStartAsync()
        {
            if (typeof(Harmony).Assembly.GetName().Version.Minor < 12)
            {
                _harmony.Patch(HarmonyTranspilersFixPatch.TargetMethod(), null, null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyTranspilersFixPatch), nameof(HarmonyTranspilersFixPatch.Transpiler))));
            }
            _harmony.PatchAll(_metadata.Assembly);

            BasicUI.GetIcons();

            if (!File.Exists(Collections.DataPath))
            {
                File.Create(Collections.DataPath);
            }
            else
            {
                await Collections.LoadExtraSongDataAsync(CancellationToken.None).ConfigureAwait(false);
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