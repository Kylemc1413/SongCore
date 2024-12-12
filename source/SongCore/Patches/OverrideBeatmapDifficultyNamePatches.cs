using SiraUtil.Affinity;

namespace SongCore.Patches
{
    // TODO: Find a way to add a limitation to the size of the text.
    internal class OverrideBeatmapDifficultyNamePatches : IAffinity
    {
        private readonly PluginConfig _config;

        private OverrideBeatmapDifficultyNamePatches(PluginConfig config)
        {
            _config = config;
        }

        [AffinityPatch(typeof(BeatmapDifficultyMethods), nameof(BeatmapDifficultyMethods.Name))]
        private void OverrideBeatmapDifficultyName(ref string __result, BeatmapDifficulty difficulty)
        {
            if (!_config.DisplayDiffLabels)
            {
                return;
            }

            __result = (difficulty switch
                {
                    BeatmapDifficulty.Easy when StandardLevelDetailViewRefreshContentPatch.currentLabels.EasyOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.EasyOverride,
                    BeatmapDifficulty.Normal when StandardLevelDetailViewRefreshContentPatch.currentLabels.NormalOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.NormalOverride,
                    BeatmapDifficulty.Hard when StandardLevelDetailViewRefreshContentPatch.currentLabels.HardOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.HardOverride,
                    BeatmapDifficulty.Expert when StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertOverride,
                    BeatmapDifficulty.ExpertPlus when StandardLevelDetailViewRefreshContentPatch.currentLabels.ExpertPlusOverride != null => StandardLevelDetailViewRefreshContentPatch.currentLabels
                        .ExpertPlusOverride,
                    _ => __result
                })
                .Replace("<", "<\u200B")
                .Replace(">", ">\u200B");
        }
    }
}
