using Harmony;
using SongCore.Utilities;
using System.Collections.Generic;
using UnityEngine;
namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapDataLoader))]
    [HarmonyPatch("GetBeatmapDataFromBeatmapSaveData", MethodType.Normal)]

    class BeatmapDataLoaderGetBeatmapDataFromBeatmapSaveData
    {
        static bool Prefix(BeatmapDataLoader __instance, List<BeatmapSaveData.NoteData> notesSaveData, List<BeatmapSaveData.ObstacleData> obstaclesSaveData, List<BeatmapSaveData.EventData> eventsSaveData, float beatsPerMinute, float shuffle, float shufflePeriod, ref BeatmapData __result)
        {

            List<BeatmapObjectData>[] array = new List<BeatmapObjectData>[4];
            List<BeatmapEventData> list = new List<BeatmapEventData>(eventsSaveData.Count);
            for (int i = 0; i < 4; i++)
            {
                array[i] = new List<BeatmapObjectData>(3000);
            }
            int num = 0;
            NoteData noteData = null;
            float num2 = -1f;
            List<NoteData> list2 = new List<NoteData>(4);
            float num3 = 0f;
            foreach (BeatmapSaveData.NoteData noteData2 in notesSaveData)
            {
                float realTimeFromBPMTime = __instance.InvokeMethod<float>("GetRealTimeFromBPMTime", new object[] { noteData2.time, beatsPerMinute, shuffle, shufflePeriod });
                if (num3 > realTimeFromBPMTime)
                {
                    Debug.LogError("Notes are not ordered.");
                }
                num3 = realTimeFromBPMTime;
                int lineIndex = noteData2.lineIndex;
                NoteLineLayer lineLayer = noteData2.lineLayer;
                NoteLineLayer startNoteLineLayer = NoteLineLayer.Base;
                if (noteData != null && noteData.lineIndex == lineIndex && Mathf.Abs(noteData.time - realTimeFromBPMTime) < 0.0001f)
                {
                    if (noteData.startNoteLineLayer == NoteLineLayer.Base)
                    {
                        startNoteLineLayer = NoteLineLayer.Upper;
                    }
                    else
                    {
                        startNoteLineLayer = NoteLineLayer.Top;
                    }
                }
                NoteType type = noteData2.type;
                NoteCutDirection cutDirection = noteData2.cutDirection;
                if (list2.Count > 0 && list2[0].time < realTimeFromBPMTime - 0.001f && type.IsBasicNote())
                {
                    __instance.InvokeMethod("ProcessBasicNotesInTimeRow", new object[] { list2, realTimeFromBPMTime });
                    num2 = list2[0].time;
                    list2.Clear();
                }
                NoteData noteData3 = new NoteData(num++, realTimeFromBPMTime, lineIndex, lineLayer, startNoteLineLayer, type, cutDirection, float.MaxValue, realTimeFromBPMTime - num2);
                int number = lineIndex;
                if (number < 0)
                    number = 0;
                if (number > 3)
                    number = 3;
                array[number].Add(noteData3);
                noteData = noteData3;
                if (noteData3.noteType.IsBasicNote())
                {
                    list2.Add(noteData);
                }
            }
            __instance.InvokeMethod("ProcessBasicNotesInTimeRow", new object[] { list2, float.MaxValue });
            foreach (BeatmapSaveData.ObstacleData obstacleData in obstaclesSaveData)
            {
                float realTimeFromBPMTime2 = __instance.InvokeMethod<float>("GetRealTimeFromBPMTime", new object[] { obstacleData.time, beatsPerMinute, shuffle, shufflePeriod });
                int lineIndex2 = obstacleData.lineIndex;
                ObstacleType type2 = obstacleData.type;
                float realTimeFromBPMTime3 = __instance.InvokeMethod<float>("GetRealTimeFromBPMTime", new object[] { obstacleData.duration, beatsPerMinute, shuffle, shufflePeriod });
                int width = obstacleData.width;
                ObstacleData item = new ObstacleData(num++, realTimeFromBPMTime2, lineIndex2, type2, realTimeFromBPMTime3, width);
                int number2 = lineIndex2;
                if (number2 < 0)
                    number2 = 0;
                if (number2 > 3)
                    number2 = 3;
                array[number2].Add(item);
            }
            foreach (BeatmapSaveData.EventData eventData in eventsSaveData)
            {
                float realTimeFromBPMTime4 = __instance.InvokeMethod<float>("GetRealTimeFromBPMTime", new object[] { eventData.time, beatsPerMinute, shuffle, shufflePeriod });
                BeatmapEventType type3 = eventData.type;
                int value = eventData.value;
                BeatmapEventData item2 = new BeatmapEventData(realTimeFromBPMTime4, type3, value);
                list.Add(item2);
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
                    return (x.time <= y.time) ? -1 : 1;
                });
                array2[j] = new BeatmapLineData();
                array2[j].beatmapObjectsData = array[j].ToArray();
            }
            __result = new BeatmapData(array2, list.ToArray());

            return false;


        }


    }


}
