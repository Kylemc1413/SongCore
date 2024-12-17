using System;
using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace SongCore.Patches
{
    [HarmonyPatch(typeof(LevelListTableCell), nameof(LevelListTableCell.SetDataFromLevelAsync))]
    internal static class LevelListTableCellDataPatch
    {
        private static void Postfix(LevelListTableCell __instance, BeatmapLevel beatmapLevel)
        {
            // Rounding BPM display for all maps, including official ones.
            __instance._songBpmText.text = Math.Round(beatmapLevel.beatsPerMinute).ToString(CultureInfo.InvariantCulture);

            var authors = string.Join(", ", beatmapLevel.allMappers.Concat(beatmapLevel.allLighters).Distinct());
            if (!string.IsNullOrWhiteSpace(authors))
            {
                __instance._songAuthorText.richText = true;
                __instance._songAuthorText.text = $"<size=80%>{beatmapLevel.songAuthorName.Trim()}</size> <size=90%>[<color=#ff69b4>{authors.Replace("<", "<\u200B").Replace(">", ">\u200B")}</color>]</size>";
            }
        }
    }
}
