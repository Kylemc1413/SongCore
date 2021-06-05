using HarmonyLib;
using SongCore.Utilities;
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
                    Collections.AddSong(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                    Collections.SaveExtraSongData();
                }
            }
        }
    }
}
[HarmonyPatch(typeof(LevelListTableCell))]
[HarmonyPatch("SetDataFromLevelAsync", MethodType.Normal)]
public class LevelListTableCellSetDataFromLevel
{
    static void Postfix(IPreviewBeatmapLevel level, bool isFavorite, ref TextMeshProUGUI ____songAuthorText, TextMeshProUGUI ____songDurationText)
    {
        if (!(level is CustomPreviewBeatmapLevel))
            return;
        var customLevel = level as CustomPreviewBeatmapLevel;
        /* Plan B
       IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew( async () => { var token = new CancellationTokenSource().Token; var audio = await level.GetPreviewAudioClipAsync(token); customLevel.SetField("_songDuration", audio.length);  ____songDurationText.text = customLevel.songDuration.MinSecDurationText(); });
        */
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
