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

    // TODO: Remove once fixed.
    [HarmonyPatch(typeof(CustomLevelLoader))]
    internal class CustomLevelLoadingPatches
    {
        [HarmonyPatch(nameof(CustomLevelLoader.CreateEnvironmentName))]
        [HarmonyPrefix]
        private static void FixEnvironmentNameCreation(ref string? environmentSerializedField)
        {
            if (environmentSerializedField == null)
            {
                environmentSerializedField = "DefaultEnvironment";
            }
        }

        [HarmonyPatch(nameof(CustomLevelLoader.CreateBeatmapLevelFromV3))]
        [HarmonyPatch(nameof(CustomLevelLoader.CreateBeatmapLevelDataFromV3))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> FixBeatmapLevelCreation(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .End()
                .MatchStartBackwards(new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == nameof(Dictionary<object, object>.Add)))
                .ThrowIfInvalid()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    Transpilers.EmitDelegate<Action<Dictionary<ValueTuple<BeatmapCharacteristicSO, BeatmapDifficulty>, BeatmapBasicData>, ValueTuple<BeatmapCharacteristicSO, BeatmapDifficulty>, BeatmapBasicData, object, object>>((dictionary, key, value, arg1, arg2) =>
                    {
                        if (dictionary.ContainsKey(key))
                        {
                            var customLevelPath = arg1 is string ? arg1 : arg2;
                            Logging.Logger.Warn($"Duplicate characteristic found while creating beatmap level: {customLevelPath}");
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                    }))
                .RemoveInstruction()
                .InstructionEnumeration();
        }
    }
}