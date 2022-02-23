using HarmonyLib;
using IPA.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch]
    internal class RemoveAssertStuffs
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var inst = instructions;

            if (__originalMethod.Name == "InsertBeatmapEventData")
            {
                // Skip the LessOrEqual and GreaterOrEqual Asserts
                if ((instructions.ElementAt(29).operand as MethodBase)?.Name == "GreaterOrEqual")
                {
                    return instructions.Skip(30);
                }
            }
            else
            {
                // Skip the IsTrue Assert
                if ((instructions.ElementAt(9).operand as MethodBase)?.Name == "IsTrue")
                {
                    return instructions.Skip(10);
                }
            }

            return instructions;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BeatmapData), nameof(BeatmapData.AddBeatmapObjectData));
            yield return AccessTools.Method(typeof(BeatmapData), nameof(BeatmapData.AddBeatmapEventData));
            yield return AccessTools.Method(typeof(BeatmapData), nameof(BeatmapData.InsertBeatmapEventData));
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
            __instance.SetField<CustomPreviewBeatmapLevel, float>("_songDuration", customPreviewBeatmapLevel.songDuration);
        }
    }

    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch("ReloadCustomLevelPackCollectionAsync", MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        private static void Prefix(Task<IBeatmapLevelPackCollection> __result)
        {
            var cancel = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().First().GetField<CancellationTokenSource, LevelFilteringNavigationController>("_cancellationTokenSource");
            cancel.Cancel();
        }
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("UpdateCustomSongs", MethodType.Normal)]
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