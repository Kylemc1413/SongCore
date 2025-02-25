using System;
using HarmonyLib;

namespace SongCore.Patches
{
    /// <summary>
    /// This patch reverses base game logic so it first looks for V3 format instead of V2.
    /// Without this patch, maps that declare both <c>version</c> and <c>_version</c> will be empty.
    /// It also makes it much faster than the original implementation.
    /// </summary>
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers), nameof(BeatmapSaveDataHelpers.GetVersion))]
    internal static class BeatmapVersionDetectionPatch
    {
        private const string VersionSearchString = "version";

        private static bool Prefix(ref Version __result, string data)
        {
            __result = GetVersion(data.AsSpan(0, 50)) ?? GetVersion(data.AsSpan()) ?? BeatmapSaveDataHelpers.noVersion;

            return false;
        }

        private static Version? GetVersion(ReadOnlySpan<char> span)
        {
            try
            {
                if (span.IndexOf(VersionSearchString) is var index && index != -1)
                {
                    span = span.Slice(index + VersionSearchString.Length + 1);
                    span = span.Slice(span.IndexOf('"') + 1);

                    if (Version.TryParse(span.Slice(0, span.IndexOf('"')), out var version))
                    {
                        return version;
                    }
                }
            }
            // Very rare case where the end of the key or value might be out of bounds.
            catch (ArgumentOutOfRangeException) { }

            return null;
        }
    }
}
