using BeatSaber.Init;
using BGLib.DotnetExtension.CommandLine;
using SiraUtil.Submissions;
using Zenject;

namespace SongCore.HarmonyPatches
{
    internal class AutoplayDisableSubmissionPatch : IInitializable
    {
        private readonly CommandLineParserResult _commandLineParserResult;
        private readonly Submission _submission;

        private AutoplayDisableSubmissionPatch(CommandLineParserResult commandLineParserResult, Submission submission)
        {
            _commandLineParserResult = commandLineParserResult;
            _submission = submission;
        }

        public void Initialize()
        {
            if (_commandLineParserResult.Contains(BSAppInit.kAutoPlayOption))
            {
                _submission.DisableScoreSubmission(nameof(SongCore), "Autoplay is enabled.");
            }
        }
    }
}
