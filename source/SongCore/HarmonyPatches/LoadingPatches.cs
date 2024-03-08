using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch(nameof(BeatmapLevelsModel.ReloadCustomLevelPackCollectionAsync), MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch(nameof(LevelFilteringNavigationController.UpdateCustomSongs), MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {
        private static bool Prefix(LevelFilteringNavigationController __instance)
        {
            if (Loader.CustomLevelsRepository == null)
            {
                return false;
            }

            __instance._customLevelPacks = Loader.CustomLevelsRepository.beatmapLevelPacks;
            IEnumerable<BeatmapLevelPack>? packs = null;
            if (__instance._ostBeatmapLevelPacks != null)
            {
                packs = __instance._ostBeatmapLevelPacks;
            }

            if (__instance._musicPacksBeatmapLevelPacks != null)
            {
                packs = packs == null ? __instance._musicPacksBeatmapLevelPacks : packs.Concat(__instance._musicPacksBeatmapLevelPacks);
            }

            if (__instance._customLevelPacks != null)
            {
                packs = packs == null ? __instance._customLevelPacks : packs.Concat(__instance._customLevelPacks);
            }

            __instance._allBeatmapLevelPacks = packs.ToArray();
            __instance._levelSearchViewController.Setup(__instance._allBeatmapLevelPacks);
            __instance.UpdateSecondChildControllerContent(__instance._selectLevelCategoryViewController.selectedLevelCategory);

            return false;
        }
    }
}