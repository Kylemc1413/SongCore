using System.Collections.Generic;
using System.Reflection.Emit;
using BeatmapSaveDataCommon;
using HarmonyLib;
using JetBrains.Annotations;

namespace SongCore.HarmonyPatches
{
    // Event10 was briefly used as an official BPM change between 1.8.0 and 1.18.0,
    // but it was never supported by custom mapping tools and later reused as a light event.
    // The code to convert these events broke a lot of maps, so we are removing it here.
    [HarmonyPatch(typeof(BeatmapSaveDataVersion2_6_0AndEarlier.BeatmapSaveData), nameof(BeatmapSaveDataVersion2_6_0AndEarlier.BeatmapSaveData.ConvertBeatmapSaveDataPreV2_5_0Inline))]
    [UsedImplicitly]
    public static class YeetLegacyBPMEventsPatch
    {
        // Skipping the:
        // if (eventData.type == BeatmapEventType.Event10)
        //    eventData = new EventData(eventData.time, BeatmapEventType.BpmChange, eventData.value, eventData.floatValue);
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool skip = false;

            for (int i = 0; i < codes.Count; i++)
            {
                // Checks for the loading onto the stack, which precedes the condition check.
                if (codes[i].opcode == OpCodes.Ldc_I4_S && i < codes.Count - 1 && codes[i + 1].opcode == OpCodes.Bne_Un)
                {
                    // Skip everything from loading Event10...
                    if ((sbyte)codes[i].operand == (sbyte)BeatmapEventType.Event10) {
                        skip = true;
                    // ...to loading proper BPM event
                    } else if (skip && (sbyte)codes[i].operand == (sbyte)BeatmapEventType.BpmChange) {
                        skip = false;
                    }
                }

                if (!skip)
                {
                    yield return codes[i];
                }
            }
        }
    }
}