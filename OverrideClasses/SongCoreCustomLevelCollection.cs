namespace SongCore.OverrideClasses
{
    public class SongCoreCustomLevelCollection : CustomBeatmapLevelCollection
    {
        public SongCoreCustomLevelCollection(CustomPreviewBeatmapLevel[] customPreviewBeatmapLevels) : base(customPreviewBeatmapLevels)
        {
        }

        public void UpdatePreviewLevels(CustomPreviewBeatmapLevel[] levels)
        {
            _customPreviewBeatmapLevels = levels;
        }
    }
}