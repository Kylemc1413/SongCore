using HarmonyLib;
using IPA.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(CustomBeatmapLevel))]
    [HarmonyPatch(new Type[] { typeof(CustomPreviewBeatmapLevel), typeof(AudioClip)})]
    [HarmonyPatch(MethodType.Constructor)]
    internal class CustomBeatmapLevelDurationPatch
    {
        static void Postfix(CustomBeatmapLevel __instance, CustomPreviewBeatmapLevel customPreviewBeatmapLevel)
        {
            __instance.SetField<CustomPreviewBeatmapLevel, float>("_songDuration", customPreviewBeatmapLevel.songDuration);
        }
    }

    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch("ReloadCustomLevelPackCollectionAsync", MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        static void Prefix(Task<IBeatmapLevelPackCollection> __result)
        {
            var cancel = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().First().GetField<CancellationTokenSource, LevelFilteringNavigationController>("_cancellationTokenSource");
            cancel.Cancel();

        }
    }
    
    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("UpdateCustomSongs", MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {

        static void Postfix(ref LevelFilteringNavigationController __instance, LevelSearchViewController ____levelSearchViewController, SelectLevelCategoryViewController ____selectLevelCategoryViewController, ref IBeatmapLevelPack[] ____ostBeatmapLevelPacks, ref IBeatmapLevelPack[] ____musicPacksBeatmapLevelPacks, ref IBeatmapLevelPack[] ____customLevelPacks, ref IBeatmapLevelPack[] ____allBeatmapLevelPacks)
        {
            if (Loader.CustomBeatmapLevelPackCollectionSO == null) return;
            ____customLevelPacks = Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks;
            List<IBeatmapLevelPack> packs = new List<IBeatmapLevelPack>();
            if (____ostBeatmapLevelPacks != null)
                packs = packs.Concat(____ostBeatmapLevelPacks).ToList();
            if (____musicPacksBeatmapLevelPacks != null)
                packs = packs.Concat(____musicPacksBeatmapLevelPacks).ToList();
            if (____customLevelPacks != null)
                packs = packs.Concat(____customLevelPacks).ToList();
            ____allBeatmapLevelPacks = packs.ToArray();
            ____levelSearchViewController.Setup(____allBeatmapLevelPacks);
            __instance.UpdateSecondChildControllerContent(____selectLevelCategoryViewController.selectedLevelCategory);
        }
    }
    /*
    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("ReloadSongListIfNeeded", MethodType.Normal)]
    internal class StopVanillaLoadingPatch3
    {

        static bool Prefix(ref LevelFilteringNavigationController __instance, ref TabBarViewController ____tabBarViewController)
        {
            __instance.GetField("_customLevelsTabBarData")?.SetField("annotatedBeatmapLevelCollections", Loader.CustomBeatmapLevelPackCollectionSO?.beatmapLevelPacks);
            return false;
        }
    }
    */

}
