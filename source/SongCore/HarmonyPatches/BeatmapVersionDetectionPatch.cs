using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace SongCore.HarmonyPatches
{
    // This patch fixes the base game implementation, which fails with maps that have no version declared.
    // Without this patch affected maps don't load when selected.
    [HarmonyPatch(typeof(BeatmapSaveDataHelpers), nameof(BeatmapSaveDataHelpers.GetVersion))]
    [UsedImplicitly]
    internal static class BeatmapVersionDetectionPatch
    {
        private static readonly Version FallbackVersion = new Version("2.0.0");

        [UsedImplicitly]
        private static bool Prefix(string data, ref Version __result)
        {
            string? version = JsonUtility.FromJson<BeatmapSaveDataHelpers.VersionSerializedData>(data).v;
            __result = version != null ? new Version(version) : FallbackVersion;

            return false;
        }
    }
}