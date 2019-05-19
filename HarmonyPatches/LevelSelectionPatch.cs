using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelPackLevelsViewController))]
    [HarmonyPatch("HandleLevelPackLevelsTableViewDidSelectLevel", MethodType.Normal)]
    class LevelPackLevelsSelectedPatch
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Prefix(LevelPackLevelsTableView tableView, IPreviewBeatmapLevel level)
        {
            if(level is CustomPreviewBeatmapLevel)
            {
                var customLevel = level as CustomPreviewBeatmapLevel;
                if (customLevel != null)
                {
                    SongCore.Collections.AddSong(Utilities.Utils.GetCustomLevelIdentifier(customLevel), customLevel.customLevelPath);
                    SongCore.Collections.SaveExtraSongData();
                }
            }
        }
    }
}
