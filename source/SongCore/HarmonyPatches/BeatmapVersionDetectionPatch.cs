using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using JetBrains.Annotations;

namespace SongCore.HarmonyPatches
{
    // This patch fixes the base game implementation, which fails with maps that have no version declared.
    // Without this patch affected maps don't load when selected. It is also 100 times faster on average.
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers), nameof(BeatmapSaveDataHelpers.GetVersion))]
    [UsedImplicitly]
    internal static class BeatmapVersionDetectionPatch
    {
        private static readonly Regex VersionRegex = new Regex(
            @"""_?version""\s*:\s*""(?<version>[0-9]+\.[0-9]+\.?[0-9]?)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Version FallbackVersion = new Version(StandardLevelInfoSaveData.kCurrentVersion);

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

    [HarmonyPatch(typeof(StandardLevelInfoSaveData.VersionCheck), nameof(StandardLevelInfoSaveData.VersionCheck.version), MethodType.Getter)]
    internal class BeatmapVersionCheckPatch
    {
        private static void Postfix(ref string __result)
        {
            // TODO: Current logic for v1 beatmaps is throwing a null ref.
            if (string.IsNullOrWhiteSpace(__result) || __result == StandardLevelInfoSaveData_V100.kCurrentVersion)
            {
                __result = StandardLevelInfoSaveData.kCurrentVersion;
            }
        }
    }
}
