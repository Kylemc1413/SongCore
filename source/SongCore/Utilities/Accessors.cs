using IPA.Utilities;

namespace SongCore.Utilities
{
    internal static class Accessors
    {
        internal static readonly FieldAccessor<CustomPreviewBeatmapLevel, float>.Accessor SongDurationAccessor =
            FieldAccessor<CustomPreviewBeatmapLevel, float>.GetAccessor(ToBackingFieldName(nameof(CustomPreviewBeatmapLevel.songDuration)));

        internal static readonly FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.Accessor BeatmapLevelCollectionAccessor =
            FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.GetAccessor(ToBackingFieldName(nameof(CustomBeatmapLevelPack.beatmapLevelCollection)));

        private static string ToBackingFieldName(string propertyName) => $"<{propertyName}>k__BackingField";
    }
}