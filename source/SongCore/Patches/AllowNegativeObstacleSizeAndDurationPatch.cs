using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SiraUtil.Affinity;
using SongCore.Utilities;

namespace SongCore.Patches
{
    /// <summary>
    /// These transpilers break Mapping Extensions patches, because the original method is calling patched methods.
    /// <list type="number">
    /// <item>
    /// <description>Transpiler runs, completed method IL is generated, resulting dynamic method is compiled.</description>
    /// </item>
    /// <item>
    /// <description>Problematic method is inlined because it's small and not marked <see cref="MethodImplOptions.NoInlining"/>.</description>
    /// </item>
    /// <item>
    /// <description>Other method is patched, dynamic method compiled, redirected, etc., but by this point it's already been inlined.</description>
    /// </item>
    /// </list>
    /// Because of this, this patch needs to be applied after the other patches.
    /// </summary>
    internal class AllowNegativeObstacleSizeAndDurationPatch : IAffinity
    {
        [AffinityPatch(typeof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter), nameof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter.Convert))]
        [AffinityTranspiler]
        private IEnumerable<CodeInstruction> RemoveReturnConditionFromV2(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Ret))
                .ThrowIfInvalid()
                .RemoveInstructionsInRange(17, 25)
                .InstructionEnumeration();
        }

        [AffinityPatch(typeof(BeatmapDataLoaderVersion3.BeatmapDataLoader.ObstacleConverter), nameof(BeatmapDataLoaderVersion3.BeatmapDataLoader.ObstacleConverter.Convert))]
        [AffinityTranspiler]
        private IEnumerable<CodeInstruction> RemoveReturnConditionFromV3(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Ret))
                .ThrowIfInvalid()
                .RemoveInstructionsInRange(17, 29)
                .InstructionEnumeration();
        }

        [AffinityPatch(typeof(BeatmapDataLoaderVersion4.ObstacleItemConverter), nameof(BeatmapDataLoaderVersion4.ObstacleItemConverter.Convert))]
        [AffinityTranspiler]
        private IEnumerable<CodeInstruction> RemoveReturnConditionFromV4(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Ret))
                .ThrowIfInvalid()
                .RemoveInstructionsInRange(23, 35)
                .InstructionEnumeration();
        }
    }
}
