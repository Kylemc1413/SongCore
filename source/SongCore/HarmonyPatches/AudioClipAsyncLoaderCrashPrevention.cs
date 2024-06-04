using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SiraUtil.Affinity;
using SongCore.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch prevents Unity from crashing when it destroys an audio clip that is playing.
    /// For more details, refer to the Unity issue tracker: https://issuetracker.unity3d.com/issues/crash-on-purecall-when-repeatedly-creating-playing-stopping-and-deleting-audio
    /// </summary>
    internal class AudioClipAsyncLoaderCrashPreventionPatch : IAffinity
    {
        private static SongPreviewPlayer _songPreviewPlayer;
        private static ICoroutineStarter _coroutineStarter;

        private AudioClipAsyncLoaderCrashPreventionPatch(SongPreviewPlayer songPreviewPlayer, ICoroutineStarter coroutineStarter)
        {
            _songPreviewPlayer = songPreviewPlayer;
            _coroutineStarter = coroutineStarter;
        }

        [AffinityPatch(typeof(AudioClipAsyncLoader), nameof(AudioClipAsyncLoader.Unload), AffinityMethodType.Normal, null, typeof(string))]
        [AffinityTranspiler]
        private IEnumerable<CodeInstruction> PreventUnityCrashWhenDestroyingAudioClip(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Ldftn))
                .ThrowIfInvalid()
                .SetOperandAndAdvance(AccessTools.Method(typeof(AudioClipAsyncLoaderCrashPreventionPatch), nameof(SafeDestroyAudioClip)))
                .InstructionEnumeration();
        }

        private static void SafeDestroyAudioClip(AudioClip audioClip)
        {
            var audioSource = _songPreviewPlayer._activeChannel >= 0 ? _songPreviewPlayer._audioSourceControllers[_songPreviewPlayer._activeChannel].audioSource : null;

            if (audioSource == null)
            {
                return;
            }

            if (audioClip == audioSource.clip && audioSource.isPlaying)
            {
                Logging.Logger.Debug(nameof(SafeDestroyAudioClip) + " will launch coroutine to destroy audio clip.");
                _coroutineStarter.StartCoroutine(DestroyAudioClipCoroutine(audioClip, audioSource));
            }
            else
            {
                Logging.Logger.Debug(nameof(SafeDestroyAudioClip) + " will destroy audio clip.");
                Object.Destroy(audioClip);
                Logging.Logger.Debug(nameof(SafeDestroyAudioClip) + " has destroyed audio clip.");
            }
        }

        private static IEnumerator DestroyAudioClipCoroutine(AudioClip audioClip, AudioSource audioSource)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying);
            Object.Destroy(audioClip);
            Logging.Logger.Debug(nameof(DestroyAudioClipCoroutine) + " has destroyed audio clip.");
        }
    }
}
