using HarmonyLib;
using IPA.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
namespace SongCore.HarmonyPatches
{
    
    [HarmonyPatch(typeof(NUnit.Framework.Assert), "IsTrue", new Type[] { typeof(bool), typeof(string), typeof(object[]) })]
    internal class WhyIsAssertMeanPatch
    {
        static void Prefix(ref bool condition)
        {
            if (condition == true) return;
            var stack = new StackTrace();
            for (int i = 0; i < stack.FrameCount; i++)
            {
                var callingMethodName = stack.GetFrame(i).GetMethod().Name;
            //    Utilities.Logging.Log($"Calling Method {i}: {callingMethodName}");
                if (callingMethodName.Contains("AddBeatmapEventData") || callingMethodName.Contains("AddBeatmapObjectData"))
                {
                    Utilities.Logging.logger.Debug("Blocking Assert Failure");
                    condition = true;
                    return;
                }
            }

        }
    }

    [HarmonyPatch(typeof(NUnit.Framework.Assert), "LessOrEqual", new Type[] { typeof(float), typeof(float), typeof(string), typeof(object[]) })]
    internal class WhyIsAssertMeanPatch2
    {
        static bool Prefix()
        {
            return false;

        }
    }
    [HarmonyPatch(typeof(NUnit.Framework.Assert), "GreaterOrEqual", new Type[] { typeof(float), typeof(float), typeof(string), typeof(object[]) })]
    internal class WhyIsAssertMeanPatch3
    {
        static bool Prefix()
        {
            return false;

        }
    }
    /*
    [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBeatmapSaveData")]
    internal class BeatmapDataLoadingEventDataSortingPatch
    {
        static void Prefix(ref List<BeatmapSaveData.EventData> eventsSaveData)
        {
            eventsSaveData.Sort(delegate (BeatmapSaveData.EventData x,
                BeatmapSaveData.EventData y)
            {
                if (x.time >= y.time)
                {
                    return 1;
                }
                return -1;
            });
        }
    }
    */
    [HarmonyPatch(typeof(BeatmapData), new Type[] { typeof(int)})]
    [HarmonyPatch(MethodType.Constructor)]
    internal class InitializePreviousAddedBeatmapEventDataTime
    {
        static void Postfix(ref float ____prevAddedBeatmapEventDataTime, ref float ____prevAddedBeatmapObjectDataTime)
        {
            ____prevAddedBeatmapEventDataTime = float.MinValue;
            ____prevAddedBeatmapObjectDataTime = float.MinValue;
        }
    }
    
    [HarmonyPatch(typeof(CustomBeatmapLevel))]
    [HarmonyPatch(new Type[] { typeof(CustomPreviewBeatmapLevel), typeof(AudioClip) })]
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
