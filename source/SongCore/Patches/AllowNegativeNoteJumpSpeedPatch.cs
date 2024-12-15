using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SongCore.Patches
{
    /// <summary>
    /// Allows negative note jump speeds, which are otherwise reset to the default NJS value.
    /// When a negative NJS is used, notes will come from behind the player and spin toward them.
    /// </summary>
    /// <example>
    /// https://beatsaver.com/maps/6cd
    /// </example>
    [HarmonyPatch(typeof(BeatmapDifficultyMethods), nameof(BeatmapDifficultyMethods.NoteJumpMovementSpeed))]
    internal static class AllowNegativeNoteJumpSpeedPatch
    {
        private static void Postfix(ref float __result, float noteJumpMovementSpeed)
        {
            if (noteJumpMovementSpeed < 0)
            {
                __result = noteJumpMovementSpeed;
            }
        }
    }

    /// <summary>
    /// By default, the provider uses the highest note jump speed value, capping it at <see cref="VariableMovementDataProvider.kMinNoteJumpMovementSpeed"/>.
    /// This patch allows it to also use the lowest NJS value when the initial one is negative, capping it at -<see cref="VariableMovementDataProvider.kMinNoteJumpMovementSpeed"/>.
    /// </summary>
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
                    Transpilers.EmitDelegate<Func<VariableMovementDataProvider, float, float>>((variableMovementDataProvider, songTime) =>
                    {
                        var noteJumpSpeed = variableMovementDataProvider._initNoteJumpMovementSpeed + variableMovementDataProvider._relativeNoteJumpSpeedInterpolation.GetValue(songTime);
                        return variableMovementDataProvider._initNoteJumpMovementSpeed > 0
                            ? Mathf.Max(noteJumpSpeed, VariableMovementDataProvider.kMinNoteJumpMovementSpeed)
                            : Mathf.Min(noteJumpSpeed, -VariableMovementDataProvider.kMinNoteJumpMovementSpeed);
                    }))
                .InstructionEnumeration();
        }
    }
}
