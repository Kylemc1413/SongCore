using HarmonyLib;
using SongCore.Utilities;
using System.Diagnostics;
using TMPro;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelCollectionViewController))]
    [HarmonyPatch("HandleLevelCollectionTableViewDidSelectLevel", MethodType.Normal)]
    class LevelPackLevelsSelectedPatch
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Prefix(LevelCollectionTableView tableView, IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel)
            {
                var customLevel = level as CustomPreviewBeatmapLevel;
                if (customLevel != null)
                {
                    //       Logging.Log(Utilities.Hashing.GetCustomLevelHash(customLevel));
                    SongCore.Collections.AddSong(Utilities.Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
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
    static void Postfix(IPreviewBeatmapLevel level, bool isFavorite, ref TextMeshProUGUI ____songAuthorText)
    {
        if (!(level is CustomPreviewBeatmapLevel))
            return;
        var customLevel = level as CustomPreviewBeatmapLevel;
        Logging.logger.Debug(level.levelID);
        ____songAuthorText.richText = true;
        if (!string.IsNullOrWhiteSpace(customLevel.levelAuthorName))
            ____songAuthorText.text = customLevel.songAuthorName + " <size=80%>[" + customLevel.levelAuthorName.Replace(@"<", "<\u200B").Replace(@">", ">\u200B") + "]</size>";



    }
}
/*
[HarmonyPatch(typeof(LevelCollectionViewController))]
[HarmonyPatch("RefreshLevelsAvailability", MethodType.Normal)]
class LevelPackLevelsSelectedPatch
{
    //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
    static bool Prefix(IBeatmapLevelPack ____pack)
    {
     //   Logging.logger.Info(____pack.packID);
        if (____pack.packID.Contains(CustomLevelLoader.kCustomLevelPackPrefixId))
            return false;
        else
            return true;
    }
}
*/
