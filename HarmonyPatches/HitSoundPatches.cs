using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned))]
    internal class NoteCutSoundEffectManagerHandleNoteWasSpawnedPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Removes the condition that filters notes that are not in chronological order.
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Brtrue))
                .RemoveInstructions(22)
                .InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(NoteCutSoundEffect), nameof(NoteCutSoundEffect.Init))]
    internal class NoteCutSoundEffectInitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Removes the PlayScheduled call from the original method.
            // Because it plays the sound instantly rather than after the passed delay.
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ret))
                .RemoveInstructions(5)
                .InstructionEnumeration();
        }

        private static void Postfix(NoteCutSoundEffect __instance, AudioSource ____audioSource, double ____startDSPTime)
        {
            __instance.StartCoroutine(PlayHitSoundCoroutine(____audioSource, ____startDSPTime));
        }

        private static IEnumerator PlayHitSoundCoroutine(AudioSource audioSource, double startDSPTime)
        {
            yield return new WaitUntil(() => AudioSettings.dspTime > startDSPTime);
            audioSource.Play();
        }
    }
}