using System.Collections.Generic;
using IPA.Utilities;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomLevelCollection : CustomBeatmapLevelCollection
    {
        private static readonly FieldAccessor<CustomBeatmapLevelCollection, IReadOnlyList<CustomPreviewBeatmapLevel>>.Accessor CustomPreviewBeatmapLevelsAccessor =
            FieldAccessor<CustomBeatmapLevelCollection, IReadOnlyList<CustomPreviewBeatmapLevel>>.GetAccessor(nameof(_customPreviewBeatmapLevels));
        public SongCoreCustomLevelCollection(CustomPreviewBeatmapLevel[] customPreviewBeatmapLevels) : base(customPreviewBeatmapLevels)
        {
        }

        public void UpdatePreviewLevels(CustomPreviewBeatmapLevel[] levels)
        {
            var that = (CustomBeatmapLevelCollection) this;
            CustomPreviewBeatmapLevelsAccessor(ref that) = levels;
        }
    }
}