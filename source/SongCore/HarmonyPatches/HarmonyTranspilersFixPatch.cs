using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch fixes an <a href="https://github.com/BepInEx/HarmonyX/issues/65">issue</a> with HarmonyX that causes it
    /// to remove certain instructions when patching a method with a transpiler. It removes the condition in
    /// <see cref="HarmonyLib.Internal.Patching.ILManipulator.WriteTo" /> that deletes existing <c>Leave</c>, <c>Endfinally</c>
    /// and <c>Endfilter</c> instructions from the patched method when they are followed by an exception block.
    /// </summary>
    // TODO: Remove this once fixed.
    internal class HarmonyTranspilersFixPatch
    {
        public static MethodBase TargetMethod() => AccessTools.Method("HarmonyLib.Internal.Patching.ILManipulator:WriteTo");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == nameof(OpCodes.Leave)))
                .ThrowIfInvalid()
                .RemoveInstructionsInRange(76, 112)
                .InstructionEnumeration();
        }
    }
}
