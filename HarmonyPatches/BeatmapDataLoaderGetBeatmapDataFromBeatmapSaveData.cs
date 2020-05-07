using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapDataLoader))]
    [HarmonyPatch("GetBeatmapDataFromBeatmapSaveData", MethodType.Normal)]

    class BeatmapDataLoaderGetBeatmapDataFromBeatmapSaveData
    {
        static readonly MethodInfo clampMethod = SymbolExtensions.GetMethodInfo(() => Clamp(0, 0, 0));
        static readonly CodeInstruction[] clampInstructions = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4_3), new CodeInstruction(OpCodes.Call, clampMethod) };
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool foundNoteLineIndex = false;
            bool foundObstacleLineIndex = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (!foundNoteLineIndex &&
                    instructionList[i].opcode == OpCodes.Ldloc_0 &&
                    instructionList[i + 1].opcode == OpCodes.Ldloc_S &&
                    ((LocalVariableInfo)instructionList[i + 1].operand).LocalIndex == 22)
                {
                    foundNoteLineIndex = true;

                    instructionList.InsertRange(i + 2, clampInstructions);
                }
                if (!foundObstacleLineIndex &&
                    instructionList[i].opcode == OpCodes.Ldloc_0 &&
                    instructionList[i + 1].opcode == OpCodes.Ldloc_S &&
                    ((LocalVariableInfo)instructionList[i + 1].operand).LocalIndex == 33)
                {
                    foundObstacleLineIndex = true;

                    instructionList.InsertRange(i + 2, clampInstructions);
                }
            }
            if (!foundNoteLineIndex || !foundObstacleLineIndex) Utilities.Logging.Log("Failed to patch BeatmapDataLoader!", IPA.Logging.Logger.Level.Error);
            return instructionList.AsEnumerable();
        }


        static int Clamp(int input, int min, int max)
        {
            return Math.Min(Math.Max(input, min), max);
        }


    }


}
