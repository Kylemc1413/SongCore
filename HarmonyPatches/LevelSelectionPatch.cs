using HarmonyLib;
using SongCore.Utilities;
using TMPro;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelCollectionViewController))]
    [HarmonyPatch("HandleLevelCollectionTableViewDidSelectLevel", MethodType.Normal)]
    internal class LevelPackLevelsSelectedPatch
    {
        private static void Prefix(LevelCollectionTableView tableView, IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                Collections.AddSong(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                Collections.SaveExtraSongData();
            }
        }
    }

    [HarmonyPatch(typeof(LevelListTableCell))]
    [HarmonyPatch("SetDataFromLevelAsync", MethodType.Normal)]
    internal class LevelListTableCellSetDataFromLevel
    {
        private static void Postfix(IPreviewBeatmapLevel level, bool isFavorite, ref TextMeshProUGUI ____songAuthorText)
        {
            if (!(level is CustomPreviewBeatmapLevel customLevel))
            {
                return;
            }

            /* Plan B
           IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew( async () => { var token = new CancellationTokenSource().Token; var audio = await level.GetPreviewAudioClipAsync(token); customLevel.SetField("_songDuration", audio.length);  ____songDurationText.text = customLevel.songDuration.MinSecDurationText(); });
            */
            ____songAuthorText.richText = true;
            if (!string.IsNullOrWhiteSpace(customLevel.levelAuthorName))
            {
                ____songAuthorText.text = $"{customLevel.songAuthorName} <size=80%>[{customLevel.levelAuthorName.Replace(@"<", "<\u200B").Replace(@">", ">\u200B")}]</size>";
            }
        }
    }
}