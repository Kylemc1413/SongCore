using HarmonyLib;
using SongCore.Utilities;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace SongCore.HarmonyPatches.AprilFools
{
    class NoteSection
    {
        public bool RightIsLeading { get; private set; } = true;
        public float RightProportion { get; private set; } = 0.5f;
        private List<NoteData> _notes = new List<NoteData>();
        public float SectionDuration { get; private set; } = 0f;
        public void AddNote(NoteData note)
        {
            _notes.Add(note);
        }
        public int NoteCount()
        {
            return _notes.Count;
        }
        public NoteData LastNote()
        {
            return _notes.LastOrDefault();
        }
        public void CalculateData()
        {
            _notes = _notes.OrderBy(x => x.time).ToList();
            if (_notes.Count < 3) return;
              NoteData first = _notes.FirstOrDefault(x => _notes.IndexOf(x) < (_notes.Count - 1) && _notes[_notes.IndexOf(x) + 1].time != x.time);
            if (first == null) return;
            RightProportion = (_notes.Where(x => x.colorType == ColorType.ColorB).Count() / (float)_notes.Count);
            RightIsLeading = first.colorType == ColorType.ColorB && RightProportion >= 0.4f;
            SectionDuration = _notes.Last().time - _notes.First().time;
       //     Logging.logger.Debug($"\nNote Section Calculated\nNote Count {_notes.Count}\nFirst Note Time{_notes.First().time}\nLast Note Time{_notes.Last().time}\nSection Duration {SectionDuration}\nRight Proportion{RightProportion}\nRight Leading {RightIsLeading}");

        }
    }
    [HarmonyPatch(typeof(BeatmapDataTransformHelper))]
    [HarmonyPatch("CreateTransformedBeatmapData")]
    class AprilFoolsLeftHandedMapsAreFinePatch
    {
        static void Prefix(IReadonlyBeatmapData beatmapData, ref IReadonlyBeatmapData __result, ref bool leftHanded)
        {
            System.DateTime time;
            bool bunbundai = false;
            try
            {
       //         var userID = 0;//BS_Utils.Gameplay.GetUserInfo.GetUserID();
       //         if (userID == 76561198182060577)
       //             bunbundai = true;
            }
            catch (System.Exception ex)
            {
                Logging.logger.Error("Error in Bunbundai Contingency");
            }
            if (IPA.Utilities.Utils.CanUseDateTimeNowSafely)
                time = System.DateTime.Now;
            else
                time = System.DateTime.UtcNow;

            if (!((time.Month == 4 && time.Day == 1) || bunbundai)) return;

            var mapNotes = new List<NoteData>();
            var objects = beatmapData.beatmapObjectsData;
            foreach (var beatmapObject in objects)
                if (beatmapObject is NoteData) mapNotes.Add(beatmapObject as NoteData);
            mapNotes.RemoveAll(x => x.colorType == ColorType.None);
            mapNotes = mapNotes.OrderBy(x => x.time).ToList();
            List<NoteSection> mapData = new List<NoteSection>();
            NoteSection lastSection = new NoteSection();
            float bpm = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level.beatsPerMinute;
            float maxNoteTimeDiff = (60f / bpm) / 2f;
            foreach(var note in mapNotes)
            {
                NoteData lastNote = lastSection.LastNote();
                if (lastNote == null)
                    lastSection.AddNote(note);
                else
                {
                    float timeDiff = note.time - lastNote.time;
                    if (timeDiff <= maxNoteTimeDiff)
                        lastSection.AddNote(note);
                    else
                    {
                        lastSection.CalculateData();
                        mapData.Add(lastSection);
                        lastSection = new NoteSection();
                        lastSection.AddNote(note);
                    }
                }
            }
            lastSection.CalculateData();
            mapData.Add(lastSection);
            mapData.RemoveAll(x => x.NoteCount() < 3);
            mapData.RemoveAll(x => x.SectionDuration == 0);
            bool alreadyMirrored = leftHanded;
            int leftNotes = mapNotes.Count(x => x.colorType == ColorType.ColorA);
            int rightNotes = mapNotes.Count(x => x.colorType == ColorType.ColorB);
            Logging.logger.Debug($"\nLeft Notes {leftNotes}\nRight Notes {rightNotes}");
            float leftTime = mapData.Where(x => !x.RightIsLeading).Sum(x => x.SectionDuration);
            float rightTime = mapData.Where(x => x.RightIsLeading).Sum(x => x.SectionDuration);
            Logging.logger.Debug($"\nLeft Time: {leftTime}\nRight Time: {rightTime}\n" +
                $"Left Sections{mapData.Count(x => !x.RightIsLeading)}\nRight Sections{mapData.Count(x => x.RightIsLeading)}");
            if((rightTime > leftTime && !alreadyMirrored) || (leftTime > rightTime && alreadyMirrored))
            {
                Logging.logger.Debug($"Mirroring Map. \"Left Handed\" Mode Active? {alreadyMirrored}");
                leftHanded = true;
               // __result = BeatmapDataMirrorTransform.CreateTransformedData(__result);

            }
        }
    }
        /*
        [HarmonyPatch(typeof(GamePause))]
        [HarmonyPatch("Pause")]
        class AprilFoolsPausePatch
        {
            static void Prefix()
            {
                System.DateTime time;
                bool bunbundai = false;
                try
                {
                    var userID = 0;//BS_Utils.Gameplay.GetUserInfo.GetUserID();
                    if (userID == 76561198182060577)
                        bunbundai = true;
                }
                catch (System.Exception ex)
                {
                    Logging.logger.Error("Error in Bunbundai Contingency");
                }
                if (IPA.Utilities.Utils.CanUseDateTimeNowSafely)
                    time = System.DateTime.Now;
                else
                    time = System.DateTime.UtcNow;

             //   if (bunbundai)
             //       BS_Utils.Gameplay.ScoreSubmission.DisableSubmission("Nice Pause bunbundai"); else 
                if (time.Month == 4 && time.Day == 1)
                    BS_Utils.Gameplay.ScoreSubmission.DisableSubmission("Nice Pause Idjot");

            }
        }
        */
    }
