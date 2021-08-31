using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapData))]
    [HarmonyPatch("AddBeatmapObjectData", MethodType.Normal)]
    internal class BeatmapDataLoaderGetBeatmapDataFromBeatmapSaveData
    {
        private static readonly MethodInfo clampMethod = SymbolExtensions.GetMethodInfo(() => Clamp(0, 0, 0));

        private static readonly CodeInstruction[] clampInstructions = {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4_3),
            new CodeInstruction(OpCodes.Call, clampMethod)
        };

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (var i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldelem_Ref)
                {
                    if (instructionList[i + 2].opcode != OpCodes.Callvirt || instructionList[i + 1].opcode != OpCodes.Ldarg_1)
                    {
                        continue;
                    }

                    Logging.Logger.Debug($"{i} Inserting Clamp Instruction for SaveData Reading");
                    instructionList.InsertRange(i, clampInstructions);
                    i += clampInstructions.Count();
                }
            }

            return instructionList.AsEnumerable();
        }

        static int Clamp(int input, int min, int max)
        {
            return Math.Min(Math.Max(input, min), max);
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectsInTimeRowProcessor))]
    [HarmonyPatch("ProcessAllNotesInTimeRow", MethodType.Normal)]
    internal class NoteProcessorClampPatch
    {
        private static readonly MethodInfo clampMethod = SymbolExtensions.GetMethodInfo(() => Clamp(0, 0, 0));

        private static readonly CodeInstruction[] clampInstructions = {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4_3),
            new CodeInstruction(OpCodes.Call, clampMethod)
        };

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (var i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldelem_Ref)
                {
                    if (instructionList[i - 1].opcode != OpCodes.Callvirt || instructionList[i - 2].opcode != OpCodes.Ldloc_2)
                    {
                        continue;
                    }

                    Logging.Logger.Debug($"{i} Inserting Clamp Instruction for Note Processor");
                    instructionList.InsertRange(i, clampInstructions);
                    i += clampInstructions.Count();
                }
            }

            return instructionList.AsEnumerable();
        }

        private static void Postfix(List<NoteData> notesInTimeRow)
        {
            if (!notesInTimeRow.Any(x => x.lineIndex > 3 || x.lineIndex < 0))
            {
                return;
            }

            Dictionary<int, List<NoteData>> notesInColumn = new Dictionary<int, List<NoteData>>();
            foreach (NoteData note in notesInTimeRow)
            {
                notesInColumn[note.lineIndex] = new List<NoteData>(3);
            }

            for (var j = 0; j < notesInTimeRow.Count; j++)
            {
                NoteData noteData = notesInTimeRow[j];
                List<NoteData> list = notesInColumn[noteData.lineIndex];
                var flag = false;
                for (var k = 0; k < list.Count; k++)
                {
                    if (list[k].noteLineLayer > noteData.noteLineLayer)
                    {
                        list.Insert(k, noteData);
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    list.Add(noteData);
                }
            }

            foreach (var list in notesInColumn.Values)
            {
                for (var m = 0; m < list.Count; m++)
                {
                    list[m].SetBeforeJumpNoteLineLayer((NoteLineLayer) m);
                }
            }
        }

        private static int Clamp(int input, int min, int max)
        {
            return Math.Min(Math.Max(input, min), max);
        }
    }


    [HarmonyPatch(typeof(BeatmapData))]
    [HarmonyPatch("beatmapObjectsData", MethodType.Getter)]
    internal class BeatmapObjectsDataClampPatch
    {
        private static bool Prefix(BeatmapLineData[] ____beatmapLinesData, BeatmapData __instance, ref IEnumerable<BeatmapObjectData> __result)
        {
            IEnumerable<BeatmapObjectData> getObjects(BeatmapLineData[] _beatmapLinesData)
            {
                BeatmapLineData[] beatmapLinesData = _beatmapLinesData;
                int[] idxs = new int[beatmapLinesData.Length];
                for (;;)
                {
                    BeatmapObjectData? minBeatmapObjectData = null;
                    var num = float.MaxValue;
                    for (var i = 0; i < beatmapLinesData.Length; i++)
                    {
                        if (idxs[i] < beatmapLinesData[i].beatmapObjectsData.Count)
                        {
                            BeatmapObjectData beatmapObjectData = beatmapLinesData[i].beatmapObjectsData[idxs[i]];
                            var time = beatmapObjectData.time;
                            if (time < num)
                            {
                                num = time;
                                minBeatmapObjectData = beatmapObjectData;
                            }
                        }
                    }

                    if (minBeatmapObjectData == null)
                    {
                        break;
                    }

                    yield return minBeatmapObjectData;
                    idxs[minBeatmapObjectData.lineIndex > 3 ? 3 : minBeatmapObjectData.lineIndex < 0 ? 0 : minBeatmapObjectData.lineIndex]++;
                    minBeatmapObjectData = null;
                }
            }

            __result = getObjects(____beatmapLinesData);
            return false;
        }
    }
}