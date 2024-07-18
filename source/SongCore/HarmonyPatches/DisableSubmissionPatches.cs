using SiraUtil.Affinity;
using SiraUtil.Submissions;

namespace SongCore.HarmonyPatches
{
    internal class DisableSubmissionPatches : IAffinity
    {
        private readonly Submission _submission;

        private Ticket? _ticket;

        private DisableSubmissionPatches(Submission submission)
        {
            _submission = submission;
        }

        [AffinityPatch(typeof(PlaybackRecord), nameof(PlaybackRecord.IsActive), AffinityMethodType.Getter)]
        private void AutoplayCheck(bool __result)
        {
            if (_ticket is null && __result)
            {
                _ticket = _submission.DisableScoreSubmission(nameof(SongCore), "Autoplay is enabled.");
            }
        }

        [AffinityPatch(typeof(ObjectsMovementRecorder), nameof(ObjectsMovementRecorder.Init))]
        private void PlaybackCheck(ObjectsMovementRecorder __instance)
        {
            if (_ticket is null && __instance._mode is ObjectsMovementRecorder.Mode.Playback)
            {
                _ticket = _submission.DisableScoreSubmission(nameof(SongCore), "Playback is enabled.");
            }
        }
    }
}
