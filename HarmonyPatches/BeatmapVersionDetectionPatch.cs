using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using JetBrains.Annotations;

namespace SongCore.HarmonyPatches
{
    // This patch fixes the base game implementation, which fails with maps that either have whitespace in their difficulty files
    // or have no version declared. Without this patch affected maps don't load when selected.
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers), nameof(BeatmapSaveDataHelpers.GetVersion))]
    [UsedImplicitly]
    internal static class BeatmapVersionDetectionPatch
    {
        private static readonly Regex VersionRegex = new Regex(
            @"""_?version""\s*:\s*""(?<version>[0-9]+\.[0-9]+\.?[0-9]?)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Version FallbackVersion = new Version("2.0.0");

        [UsedImplicitly]
        private static bool Prefix(string data, ref Version __result)
        {
            var truncatedText = data.Substring(0, 50);
            var match = VersionRegex.Match(truncatedText);
            if (!match.Success)
            {
                __result = FallbackVersion;
                return false;
            }

            __result = new Version(match.Groups["version"].Value);
            return false;
        }
    }
}