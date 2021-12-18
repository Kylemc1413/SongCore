using HarmonyLib;
using TMPro;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelListTableCell))]
    [HarmonyPatch("SetDataFromLevelAsync", MethodType.Normal)]
    internal class LevelListTableCellSetDataFromLevel
    {
        private static void Postfix(IPreviewBeatmapLevel level, bool isFavorite, ref TextMeshProUGUI ____songAuthorText, ref TextMeshProUGUI ____songBpmText)
        {
            // Rounding BPM display for all maps, including official ones
            ____songBpmText.text = System.Math.Round(level.beatsPerMinute).ToString();

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
                ____songAuthorText.text = $"<size=80%>{customLevel.songAuthorName}</size> <size=90%>[<color=#89ff89>{customLevel.levelAuthorName.Replace(@"<", "<\u200B").Replace(@">", ">\u200B")}</color>]</size>";
            }
        }
    }
}