using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    internal class FixAudioClipAsyncLoaderCrashPatch : IAffinity
    {
        private static ICoroutineStarter _coroutineStarter;

        private FixAudioClipAsyncLoaderCrashPatch(ICoroutineStarter coroutineStarter)
        {
            _coroutineStarter = coroutineStarter;
        }

        [AffinityPatch(typeof(AudioClipAsyncLoader), nameof(AudioClipAsyncLoader.Unload), AffinityMethodType.Normal, null, typeof(string))]
        [AffinityTranspiler]
        private IEnumerable<CodeInstruction> PreventUnityCrashWhenDestroyingAudioClip(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Ldftn))
                .ThrowIfInvalid()
                .SetOperandAndAdvance(AccessTools.Method(typeof(FixAudioClipAsyncLoaderCrashPatch), nameof(SafeDestroyAudioClip)))
                .InstructionEnumeration();
        }

        private static void SafeDestroyAudioClip(AudioClip audioClip)
        {
            // For extra safety, look for all audio sources that might be playing it.
            var audioSources = Object.FindObjectsOfType<AudioSource>().Where(s => s.clip == audioClip && s.isPlaying).ToArray();
            if (audioSources.Length > 0)
            {
                foreach (var audioSource in audioSources)
                {
                    Logging.Logger.Debug("Destroying audio clip with a coroutine.");
                    _coroutineStarter.StartCoroutine(DestroyAudioClipCoroutine(audioClip, audioSource));
                }
            }
            else
            {
                Logging.Logger.Debug("Destroying audio clip.");
                Object.Destroy(audioClip);
                Logging.Logger.Debug("Audio clip destroyed.");
            }
        }

        private static IEnumerator DestroyAudioClipCoroutine(AudioClip audioClip, AudioSource audioSource)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying);
            Object.Destroy(audioClip);
            Logging.Logger.Debug("Audio clip destroyed by the coroutine.");
        }
    }
}
