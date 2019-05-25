using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using TMPro;
using SongCore.Utilities;
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
                    Logging.Log(Utilities.Utils.GetCustomLevelHash(customLevel));
                    SongCore.Collections.AddSong(Utilities.Utils.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                    SongCore.Collections.SaveExtraSongData();
                }
            }
        }
    }
}
[HarmonyPatch(typeof(LevelListTableCell))]
[HarmonyPatch("SetDataFromLevelAsync", MethodType.Normal)]
public class LevelListTableCellSetDataFromLevel
{
    static void Postfix(IPreviewBeatmapLevel level, ref TextMeshProUGUI ____authorText)
    {
        if (!(level is CustomPreviewBeatmapLevel))
            return;
        var customLevel = level as CustomPreviewBeatmapLevel;

        ____authorText.richText = true;
        //     ____authorText.overflowMode = TextOverflowModes.Overflow;
        if (!string.IsNullOrWhiteSpace(customLevel.levelAuthorName)) 
        ____authorText.text = customLevel.songAuthorName + " <size=80%>[" +customLevel.levelAuthorName + "]</size>";



    }
}
