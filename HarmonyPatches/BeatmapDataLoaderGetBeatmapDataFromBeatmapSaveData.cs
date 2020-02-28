using HarmonyLib;
using SongCore.Utilities;
using System.Collections.Generic;
using UnityEngine;
namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapDataLoader))]
    [HarmonyPatch("GetBeatmapDataFromBeatmapSaveData", MethodType.Normal)]

    class BeatmapDataLoaderGetBeatmapDataFromBeatmapSaveData
    {
            // Token: 0x0600041E RID: 1054 RVA: 0x000103E0 File Offset: 0x0000E5E0
            private struct BPMChangeData
            {
                // Token: 0x0600041E RID: 1054 RVA: 0x000103E0 File Offset: 0x0000E5E0
                public BPMChangeData(float bpmChangeStartTime, float bpmChangeStartBPMTime, float bpm)
                {
                    this.bpmChangeStartTime = bpmChangeStartTime;
                    this.bpmChangeStartBPMTime = bpmChangeStartBPMTime;
                    this.bpm = bpm;
                }

                // Token: 0x0400046F RID: 1135
                public readonly float bpmChangeStartTime;

                // Token: 0x04000470 RID: 1136
                public readonly float bpmChangeStartBPMTime;

                // Token: 0x04000471 RID: 1137
                public readonly float bpm;
            }

            static bool Prefix(ref BeatmapDataLoader __instance, List<BeatmapSaveData.NoteData> notesSaveData, List<BeatmapSaveData.ObstacleData> obstaclesSaveData, List<BeatmapSaveData.EventData> eventsSaveData, float startBPM, float shuffle, float shufflePeriod, ref Object ____notesInTimeRowProcessor, ref BeatmapData __result)
        {

            List<BeatmapObjectData>[] array = new List<BeatmapObjectData>[4];
            List<BeatmapEventData> list = new List<BeatmapEventData>(eventsSaveData.Count);
            List<BPMChangeData> list2 = new List<BPMChangeData>();
            list2.Add(new BPMChangeData(0f, 0f, startBPM));
            BPMChangeData bpmchangeData = list2[0];
            foreach (BeatmapSaveData.EventData eventData in eventsSaveData)
            {
                if (eventData.type.IsBPMChangeEvent())
                {
                    float time = eventData.time;
                    int value = eventData.value;
                    float bpmChangeStartTime = bpmchangeData.bpmChangeStartTime + __instance.GetRealTimeFromBPMTime(time - bpmchangeData.bpmChangeStartBPMTime, (float)value, shuffle, shufflePeriod);
                    list2.Add(new BPMChangeData(bpmChangeStartTime, time, (float)value));
                }
            }
            for (int i = 0; i < 4; i++)
            {
                array[i] = new List<BeatmapObjectData>(3000);
            }
            int num = 0;
            float num2 = -1f;
            List<NoteData> list3 = new List<NoteData>(4);
            List<NoteData> list4 = new List<NoteData>(4);
            int num3 = 0;
            foreach (BeatmapSaveData.NoteData noteData in notesSaveData)
            {
                float time2 = noteData.time;
                while (num3 < list2.Count - 1 && list2[num3 + 1].bpmChangeStartBPMTime < time2)
                {
                    num3++;
                }
                BPMChangeData bpmchangeData2 = list2[num3];
                float num4 = bpmchangeData2.bpmChangeStartTime + __instance.GetRealTimeFromBPMTime(time2 - bpmchangeData2.bpmChangeStartBPMTime, bpmchangeData2.bpm, shuffle, shufflePeriod);
                int lineIndex = noteData.lineIndex;
                NoteLineLayer lineLayer = noteData.lineLayer;
                NoteLineLayer startNoteLineLayer = NoteLineLayer.Base;
                NoteType type = noteData.type;
                NoteCutDirection cutDirection = noteData.cutDirection;
                if (list3.Count > 0 && list3[0].time < num4 - 0.001f && type.IsBasicNote())
                {
                    ____notesInTimeRowProcessor.InvokeMethod("ProcessBasicNotesInTimeRow", list3, num4);
                    num2 = list3[0].time;
                    list3.Clear();
                }
                if (list4.Count > 0 && list4[0].time < num4 - 0.001f)
                {
                    ____notesInTimeRowProcessor.InvokeMethod("ProcessNotesInTimeRow", list4);
                    list4.Clear();
                }
                NoteData noteData2 = new NoteData(num++, num4, lineIndex, lineLayer, startNoteLineLayer, type, cutDirection, float.MaxValue, num4 - num2);
                int lineNum = lineIndex > 3 ? 3 : lineIndex < 0 ? 0 : lineIndex;
                array[lineNum].Add(noteData2);
                NoteData item = noteData2;
                if (noteData2.noteType.IsBasicNote())
                {
                    list3.Add(item);
                }
                list4.Add(item);
            }
            ____notesInTimeRowProcessor.InvokeMethod("ProcessBasicNotesInTimeRow", list3, float.MaxValue);
            ____notesInTimeRowProcessor.InvokeMethod("ProcessNotesInTimeRow", list4);
            num3 = 0;
            foreach (BeatmapSaveData.ObstacleData obstacleData in obstaclesSaveData)
            {
                float time3 = obstacleData.time;
                while (num3 < list2.Count - 1 && list2[num3 + 1].bpmChangeStartBPMTime < time3)
                {
                    num3++;
                }
                BPMChangeData bpmchangeData3 = list2[num3];
                float time4 = bpmchangeData3.bpmChangeStartTime + __instance.GetRealTimeFromBPMTime(time3 - bpmchangeData3.bpmChangeStartBPMTime, bpmchangeData3.bpm, shuffle, shufflePeriod);
                int lineIndex2 = obstacleData.lineIndex;
                ObstacleType type2 = obstacleData.type;
                float realTimeFromBPMTime = __instance.GetRealTimeFromBPMTime(obstacleData.duration, startBPM, shuffle, shufflePeriod);
                int width = obstacleData.width;
                ObstacleData item2 = new ObstacleData(num++, time4, lineIndex2, type2, realTimeFromBPMTime, width);
                int lineNum = lineIndex2 > 3 ? 3 : lineIndex2 < 0 ? 0 : lineIndex2;
                array[lineNum].Add(item2);
            }
            foreach (BeatmapSaveData.EventData eventData2 in eventsSaveData)
            {
                float time5 = eventData2.time;
                while (num3 < list2.Count - 1 && list2[num3 + 1].bpmChangeStartBPMTime < time5)
                {
                    num3++;
                }
                BPMChangeData bpmchangeData4 = list2[num3];
                float time6 = bpmchangeData4.bpmChangeStartTime + __instance.GetRealTimeFromBPMTime(time5 - bpmchangeData4.bpmChangeStartBPMTime, bpmchangeData4.bpm, shuffle, shufflePeriod);
                BeatmapEventType type3 = eventData2.type;
                int value2 = eventData2.value;
                BeatmapEventData item3 = new BeatmapEventData(time6, type3, value2);
                list.Add(item3);
            }
            if (list.Count == 0)
            {
                list.Add(new BeatmapEventData(0f, BeatmapEventType.Event0, 1));
                list.Add(new BeatmapEventData(0f, BeatmapEventType.Event4, 1));
            }
            BeatmapLineData[] array2 = new BeatmapLineData[4];
            for (int j = 0; j < 4; j++)
            {
                array[j].Sort(delegate (BeatmapObjectData x, BeatmapObjectData y)
                {
                    if (x.time == y.time)
                    {
                        return 0;
                    }
                    if (x.time <= y.time)
                    {
                        return -1;
                    }
                    return 1;
                });
                array2[j] = new BeatmapLineData();
                array2[j].beatmapObjectsData = array[j].ToArray();
            }
            __result = new BeatmapData(array2, list.ToArray());
            return false;
        }


    }


}
