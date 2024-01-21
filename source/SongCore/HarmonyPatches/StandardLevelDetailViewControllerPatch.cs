using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BGLib.Polyglot;
using HarmonyLib;
using MonoMod.Utils;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This patch catches all exceptions and displays an error message to the user
    /// in the <see cref="StandardLevelDetailView"/> when the game is loading beatmap levels.
    /// </summary>
    // TODO: Make this use MethodType.Async once supported.
    [HarmonyPatch]
    internal class StandardLevelDetailViewControllerPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowLoadingAndDoSomething)).GetStateMachineTarget();

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(i => i.blocks.FirstOrDefault()?.blockType == ExceptionBlockType.BeginCatchBlock))
                .ThrowIfInvalid();
            codeMatcher.Instruction.blocks[0].catchType = typeof(Exception);
            return codeMatcher
                .SetOpcodeAndAdvance(OpCodes.Stloc_3)
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    Transpilers.EmitDelegate<Action<StandardLevelDetailViewController, Exception>>((standardLevelDetailViewController, ex) =>
                    {
                        var handled = false;
                        switch (ex)
                        {
                            case OperationCanceledException:
                                // Base game skips those.
                                return;
                            case ArgumentOutOfRangeException:
                            {
                                if (ex.StackTrace.Contains(nameof(BeatmapCharacteristicSegmentedControlController)))
                                {
                                    const string errorText = "Error loading beatmap. Missing or unknown characteristic.";
                                    standardLevelDetailViewController.ShowContent(StandardLevelDetailViewController.ContentType.Error, errorText);
                                    Logging.Logger.Error(errorText);
                                    handled = true;
                                }

                                break;
                            }
                            case ArgumentNullException:
                            {
                                if (ex.StackTrace.Contains(nameof(BeatmapSaveDataHelpers.GetVersion)))
                                {
                                    const string errorText = "Error loading beatmap version.";
                                    standardLevelDetailViewController.ShowContent(StandardLevelDetailViewController.ContentType.Error, errorText);
                                    Logging.Logger.Error(errorText);
                                    handled = true;
                                }

                                break;
                            }
                        }

                        if (!handled)
                        {
                            standardLevelDetailViewController.ShowContent(StandardLevelDetailViewController.ContentType.Error, Localization.Get(StandardLevelDetailViewController.kLoadingDataErrorLocalizationKey));
                        }

                        Logging.Logger.Error(ex);
                    }))
                .InstructionEnumeration();
        }
    }
}
