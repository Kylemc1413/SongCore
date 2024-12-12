using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SongCore.Patches
{
    [HarmonyPatch(typeof(BeatmapDifficultyMethods), nameof(BeatmapDifficultyMethods.NoteJumpMovementSpeed))]
    internal static class AllowNegativeNjsValuesPatch
    {
        private static void Postfix(ref float __result, float noteJumpMovementSpeed)
        {
            if (noteJumpMovementSpeed < 0)
            {
                __result = noteJumpMovementSpeed;
            }
        }
    }

    [HarmonyPatch(typeof(VariableMovementDataProvider), nameof(VariableMovementDataProvider.ManualUpdate))]
    internal static class VariableMovementDataProviderPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .Start()
                .RemoveInstructions(10)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    Transpilers.EmitDelegate<Func<VariableMovementDataProvider, float, float>>((variableMovementDataProvider, songTime) => variableMovementDataProvider._initNoteJumpMovementSpeed > 0
                        ? Mathf.Max(variableMovementDataProvider._initNoteJumpMovementSpeed + variableMovementDataProvider._relativeNoteJumpSpeedInterpolation.GetValue(songTime), VariableMovementDataProvider.kMinNoteJumpMovementSpeed)
                        : Mathf.Min(variableMovementDataProvider._initNoteJumpMovementSpeed + variableMovementDataProvider._relativeNoteJumpSpeedInterpolation.GetValue(songTime), -VariableMovementDataProvider.kMinNoteJumpMovementSpeed)))
                .InstructionEnumeration();
        }
    }
}
