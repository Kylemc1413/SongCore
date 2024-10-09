using System;
using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelListTableCell), nameof(LevelListTableCell.SetDataFromLevelAsync))]
    internal class LevelListTableCellSetDataFromLevelAsyncPatch
    {
        private static void Postfix(LevelListTableCell __instance, BeatmapLevel beatmapLevel)
        {
            // Rounding BPM display for all maps, including official ones.
            __instance._songBpmText.text = Math.Round(beatmapLevel.beatsPerMinute).ToString(CultureInfo.InvariantCulture);

            var authors = beatmapLevel.allMappers.Concat(beatmapLevel.allLighters).Distinct().Join();
            if (!string.IsNullOrWhiteSpace(authors))
            {
                var mapperColor = Plugin.Configuration.GreenMapperColor ? "89ff89" : "ff69b4";
                __instance._songAuthorText.richText = true;
                __instance._songAuthorText.text = $"<size=80%>{beatmapLevel.songAuthorName.Trim()}</size> <size=90%>[<color=#{mapperColor}>{authors.Replace("<", "<\u200B").Replace(">", ">\u200B")}</color>]</size>";
            }
        }
    }
}
