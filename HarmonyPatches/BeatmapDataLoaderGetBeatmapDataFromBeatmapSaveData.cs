using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
namespace SongCore.HarmonyPatches
{
    
    [HarmonyPatch(typeof(BeatmapData))]
    [HarmonyPatch("AddBeatmapObjectData", MethodType.Normal)]
    
    class BeatmapDataLoaderGetBeatmapDataFromBeatmapSaveData
    {
        static readonly MethodInfo clampMethod = SymbolExtensions.GetMethodInfo(() => Clamp(0, 0, 0));
        static readonly CodeInstruction[] clampInstructions = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4_3), new CodeInstruction(OpCodes.Call, clampMethod) };

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldelem_Ref)
                {
                    if (instructionList[i + 2].opcode != OpCodes.Callvirt || instructionList[i + 1].opcode != OpCodes.Ldarg_1)
                    {
                        continue;
                    }
                //    Type varType = ((LocalVariableInfo)(instructionList[i - 2].operand)).LocalType;
                //    if (varType == typeof(BeatmapObjectData))
                //    {
                        
                        Utilities.Logging.logger.Debug($"{i}Inserting Clamp Instruction");
                        instructionList.InsertRange(i, clampInstructions);
                        i += clampInstructions.Count();
                //    }

                }
            }

            return instructionList.AsEnumerable();
        }

        static int Clamp(int input, int min, int max)
        {
            return Math.Min(Math.Max(input, min), max);
        }
    }

    [HarmonyPatch(typeof(NotesInTimeRowProcessor))]
    [HarmonyPatch("ProcessAllNotesInTimeRow", MethodType.Normal)]

    class NoteProcessorClampPatch
    {
        static readonly MethodInfo clampMethod = SymbolExtensions.GetMethodInfo(() => Clamp(0, 0, 0));
        static readonly CodeInstruction[] clampInstructions = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4_3), new CodeInstruction(OpCodes.Call, clampMethod) };

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldelem_Ref)
                {
                    if (instructionList[i - 1].opcode != OpCodes.Callvirt || instructionList[i - 2].opcode != OpCodes.Ldloc_2)
                    {
                        continue;
                    }
                    //    Type varType = ((LocalVariableInfo)(instructionList[i - 2].operand)).LocalType;
                    //    if (varType == typeof(BeatmapObjectData))
                    //    {

                    Utilities.Logging.logger.Debug($"{i}Inserting Clamp Instruction");
                    instructionList.InsertRange(i, clampInstructions);
                    i += clampInstructions.Count();
                    //    }

                }
            }

            return instructionList.AsEnumerable();
        }

        static int Clamp(int input, int min, int max)
        {
            return Math.Min(Math.Max(input, min), max);
        }
    }

}