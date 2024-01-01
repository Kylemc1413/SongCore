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
            Accessors.SongDurationAccessor(ref thisInstance) = customPreviewBeatmapLevel.songDuration;
        }
    }

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
            if (Loader.CustomBeatmapLevelPackCollectionSO == null)
            {
                return false;
            }

            __instance._customLevelPacks = Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks;
            IEnumerable<IBeatmapLevelPack>? packs = null;
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