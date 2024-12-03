using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeatmapSaveDataCommon;
using HarmonyLib;

namespace SongCore.Patches
{
    // Event10 was briefly used as an official BPM change between 1.8.0 and 1.18.0,
    // but it was never supported by custom mapping tools and later reused as a light event.
    // The code to convert these events broke a lot of maps, so we are removing it here.
    [HarmonyPatch(typeof(BeatmapSaveDataVersion2_6_0AndEarlier.BeatmapSaveData), nameof(BeatmapSaveDataVersion2_6_0AndEarlier.BeatmapSaveData.ConvertBeatmapSaveDataPreV2_5_0Inline))]
    internal static class YeetLegacyBPMEventsPatch
    {
        // Skipping the:
        // if (eventData.type == BeatmapEventType.Event10)
        //    eventData = new EventData(eventData.time, BeatmapEventType.BpmChange, eventData.value, eventData.floatValue);
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToArray();
            for (int i = 0; i < codes.Length; i++)
            {
                int targetIndex = i + 3;
                // Checks for the loading onto the stack, which precedes the condition check.
                // Skip it and `type` fetching (2 instructions as well) by branching to the condition end.
                if (targetIndex < codes.Length &&
                    codes[targetIndex - 1].opcode == OpCodes.Ldc_I4_S &&
                    (sbyte)codes[targetIndex - 1].operand == (sbyte)BeatmapEventType.LegacyBpmEventType &&
                    codes[targetIndex].opcode == OpCodes.Bne_Un)
                {
                    yield return new CodeInstruction(OpCodes.Br_S, codes[targetIndex].operand);
                }
                yield return codes[i];
            }
        }
    }
}