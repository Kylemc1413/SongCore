using HarmonyLib;
using SongCore.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch("ReloadCustomLevelPackCollectionAsync", MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        static void Prefix(Task<IBeatmapLevelPackCollection> __result)
        {
            var cancel = new CancellationTokenSource();
            cancel.Cancel();

        }
    }
    
    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("UpdateCustomSongs", MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {

        static void Postfix(ref LevelFilteringNavigationController __instance, SelectLevelCategoryViewController ____selectLevelCategoryViewController, ref IBeatmapLevelPack[] ____ostBeatmapLevelPacks, ref IBeatmapLevelPack[] ____musicPacksBeatmapLevelPacks, ref IBeatmapLevelPack[] ____customLevelPacks, ref IBeatmapLevelPack[] ____allBeatmapLevelPacks)
        {
            ____customLevelPacks = Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks;
            List<IBeatmapLevelPack> packs = new List<IBeatmapLevelPack>();
            if (____ostBeatmapLevelPacks != null)
                packs = packs.Concat(____ostBeatmapLevelPacks).ToList();
            if (____musicPacksBeatmapLevelPacks != null)
                packs = packs.Concat(____musicPacksBeatmapLevelPacks).ToList();
            if (____customLevelPacks != null)
                packs = packs.Concat(____customLevelPacks).ToList();
            ____allBeatmapLevelPacks = packs.ToArray();
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
