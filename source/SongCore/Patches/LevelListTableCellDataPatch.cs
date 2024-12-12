using System;
using System.Globalization;
using System.Linq;
using SiraUtil.Affinity;

namespace SongCore.Patches
{
    internal class LevelListTableCellDataPatch : IAffinity
    {
        private readonly PluginConfig _config;

        private LevelListTableCellDataPatch(PluginConfig config)
        {
            _config = config;
        }

        [AffinityPatch(typeof(LevelListTableCell), nameof(LevelListTableCell.SetDataFromLevelAsync))]
        private void CustomizeData(LevelListTableCell __instance, BeatmapLevel beatmapLevel)
        {
            // Rounding BPM display for all maps, including official ones.
            __instance._songBpmText.text = Math.Round(beatmapLevel.beatsPerMinute).ToString(CultureInfo.InvariantCulture);

            var authors = string.Join(", ", beatmapLevel.allMappers.Concat(beatmapLevel.allLighters).Distinct());
            if (!string.IsNullOrWhiteSpace(authors))
            {
                var mapperColor = _config.GreenMapperColor ? "89ff89" : "ff69b4";
                __instance._songAuthorText.richText = true;
                __instance._songAuthorText.text = $"<size=80%>{beatmapLevel.songAuthorName.Trim()}</size> <size=90%>[<color=#{mapperColor}>{authors.Replace("<", "<\u200B").Replace(">", ">\u200B")}</color>]</size>";
            }
        }
    }
}
