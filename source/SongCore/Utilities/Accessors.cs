using IPA.Utilities;

namespace SongCore.Utilities
{
    internal static class Accessors
    {
        internal static readonly FieldAccessor<BeatmapLevel, string>.Accessor LevelIDAccessor =
            FieldAccessor<BeatmapLevel, string>.GetAccessor(nameof(BeatmapLevel.levelID));

        internal static readonly FieldAccessor<BeatmapLevel, float>.Accessor SongDurationAccessor =
            FieldAccessor<BeatmapLevel, float>.GetAccessor(nameof(BeatmapLevel.songDuration));

        internal static readonly FieldAccessor<BeatmapLevel, float>.Accessor PreviewDurationAccessor =
            FieldAccessor<BeatmapLevel, float>.GetAccessor(nameof(BeatmapLevel.previewDuration));

        internal static readonly FieldAccessor<BeatmapLevelPack, BeatmapLevel[]>.Accessor BeatmapLevelsAccessor =
            FieldAccessor<BeatmapLevelPack, BeatmapLevel[]>.GetAccessor(nameof(BeatmapLevelPack.beatmapLevels));
    }
}