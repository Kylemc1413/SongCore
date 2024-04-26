using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapSaveDataCommon;
using BeatmapSaveDataVersion2_6_0AndEarlier;
using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BpmTimeProcessor), MethodType.Constructor, new Type[] { typeof(float), typeof(IReadOnlyList<EventData>) })]
    public static class BpmTimeProcessorPatch {
        public static void Postfix(BpmTimeProcessor __instance, float startBpm, IReadOnlyList<EventData> events)
        {
            var bpmEvents = events.Where(e => e.type == BeatmapEventType.BpmChange).ToArray();
            bool startsAtZero = bpmEvents.Length > 0 && bpmEvents[0].time == 0.0;
            if (startsAtZero) {
                startBpm = bpmEvents[0].floatValue;
            }
            __instance._bpmChangeDataList[0] = new BpmTimeProcessor.BpmChangeData(0.0f, 0.0f, startBpm);
            int bpmIndex = 1;
            for (int index = startsAtZero ? 1 : 0; index < bpmEvents.Length; ++index)
            {
                BpmTimeProcessor.BpmChangeData prevBpmChangeData = __instance._bpmChangeDataList[bpmIndex - 1];
                float time = bpmEvents[index].time;
                float floatValue = bpmEvents[index].floatValue;
                __instance._bpmChangeDataList[bpmIndex] = new BpmTimeProcessor.BpmChangeData(BpmTimeProcessor.CalculateTime(prevBpmChangeData, time), time, floatValue);
                bpmIndex++;
            }
        }
    }
}
