namespace SongCore.OverrideClasses
{
    public class SongCoreCustomLevelCollection : CustomBeatmapLevelCollection
    {
        //    public readonly List<CustomPreviewBeatmapLevel> _levels = new List<CustomPreviewBeatmapLevel>();
        public SongCoreCustomLevelCollection(CustomPreviewBeatmapLevel[] customPreviewBeatmapLevels) : base(customPreviewBeatmapLevels)
        {
        }

        public void UpdatePreviewLevels(CustomPreviewBeatmapLevel[] levels)
        {
            _customPreviewBeatmapLevels = levels;
        }
    }
}
