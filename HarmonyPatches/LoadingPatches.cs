using HarmonyLib;
using IPA.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    /*
    [HarmonyPatch(typeof(NUnit.Framework.Assert), nameof(NUnit.Framework.Assert.IsTrue), typeof(bool), typeof(string), typeof(object[]))]
    internal class WhyIsAssertMeanPatch
    {
        private static void Prefix(ref bool condition)
        {
            if (condition)
            {
                return;
            }

            var stack = new StackTrace();
            for (var i = 0; i < stack.FrameCount; i++)
            {
                var callingMethodName = stack.GetFrame(i).GetMethod().Name;
                if (callingMethodName.Contains("AddBeatmapEventData") || callingMethodName.Contains("AddBeatmapObjectData"))
                {
                    Utilities.Logging.Logger.Debug("Blocking Assert Failure");
                    condition = true;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(NUnit.Framework.Assert), "LessOrEqual", typeof(float), typeof(float), typeof(string), typeof(object[]))]
    internal class WhyIsAssertMeanPatch2
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(NUnit.Framework.Assert), "GreaterOrEqual", typeof(float), typeof(float), typeof(string), typeof(object[]))]
    internal class WhyIsAssertMeanPatch3
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(BeatmapData), new[]
    {
        typeof(int)
    })]
    [HarmonyPatch(MethodType.Constructor)]
    internal class InitializePreviousAddedBeatmapEventDataTime
    {
        private static void Postfix(ref float ____prevAddedBeatmapEventDataTime, ref float ____prevAddedBeatmapObjectDataTime)
        {
            ____prevAddedBeatmapEventDataTime = float.MinValue;
            ____prevAddedBeatmapObjectDataTime = float.MinValue;
        }
    }
    */
    [HarmonyPatch(typeof(CustomBeatmapLevel))]
    [HarmonyPatch(new[]
    {
        typeof(CustomPreviewBeatmapLevel)
    })]
    [HarmonyPatch(MethodType.Constructor)]
    internal class CustomBeatmapLevelDurationPatch
    {
        private static void Postfix(CustomBeatmapLevel __instance, CustomPreviewBeatmapLevel customPreviewBeatmapLevel)
        {
            var thisInstance = (CustomPreviewBeatmapLevel) __instance;
            Accessors.SongDurationSetter(ref thisInstance) = customPreviewBeatmapLevel.songDuration;
        }
    }

    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch(nameof(BeatmapLevelsModel.ReloadCustomLevelPackCollectionAsync), MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        private static void Prefix(Task<IBeatmapLevelPackCollection> __result)
        {
            var cancel = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().First().GetField<CancellationTokenSource, LevelFilteringNavigationController>("_cancellationTokenSource");
            cancel.Cancel();
        }
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch(nameof(LevelFilteringNavigationController.UpdateCustomSongs), MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {
        private static void Postfix(ref LevelFilteringNavigationController __instance, LevelSearchViewController ____levelSearchViewController,
            SelectLevelCategoryViewController ____selectLevelCategoryViewController, ref IBeatmapLevelPack[] ____ostBeatmapLevelPacks, ref IBeatmapLevelPack[] ____musicPacksBeatmapLevelPacks,
            ref IBeatmapLevelPack[] ____customLevelPacks, ref IBeatmapLevelPack[] ____allBeatmapLevelPacks)
        {
            if (Loader.CustomBeatmapLevelPackCollectionSO == null)
            {
                return;
            }

            ____customLevelPacks = Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks;
            List<IBeatmapLevelPack> packs = new List<IBeatmapLevelPack>();
            if (____ostBeatmapLevelPacks != null)
            {
                packs = packs.Concat(____ostBeatmapLevelPacks).ToList();
            }

            if (____musicPacksBeatmapLevelPacks != null)
            {
                packs = packs.Concat(____musicPacksBeatmapLevelPacks).ToList();
            }

            if (____customLevelPacks != null)
            {
                packs = packs.Concat(____customLevelPacks).ToList();
            }

            ____allBeatmapLevelPacks = packs.ToArray();
            ____levelSearchViewController.Setup(____allBeatmapLevelPacks);
            __instance.UpdateSecondChildControllerContent(____selectLevelCategoryViewController.selectedLevelCategory);
        }
    }
}