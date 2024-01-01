using System.Globalization;
using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelListTableCell))]
    [HarmonyPatch(nameof(LevelListTableCell.SetDataFromLevelAsync), MethodType.Normal)]
    internal class LevelListTableCellSetDataFromLevel
    {
        private static void Postfix(LevelListTableCell __instance, IPreviewBeatmapLevel level)
        {
            // Rounding BPM display for all maps, including official ones
            __instance._songBpmText.text = System.Math.Round(level.beatsPerMinute).ToString(CultureInfo.InvariantCulture);

            /* Plan B
           IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew( async () => { var token = new CancellationTokenSource().Token; var audio = await level.GetPreviewAudioClipAsync(token); customLevel.SetField("_songDuration", audio.length);  ____songDurationText.text = customLevel.songDuration.MinSecDurationText(); });
            */
            if (!string.IsNullOrWhiteSpace(level.levelAuthorName))
            {
                __instance._songAuthorText.richText = true;
                //Get PinkCore'd

                string mapperColor = Plugin.Configuration.GreenMapperColor ? "89ff89" : "ff69b4";

                __instance._songAuthorText.text = $"<size=80%>{level.songAuthorName}</size> <size=90%>[<color=#{mapperColor}>{level.levelAuthorName.Replace(@"<", "<\u200B").Replace(@">", ">\u200B")}</color>]</size>";
            }
        }
    }
}
