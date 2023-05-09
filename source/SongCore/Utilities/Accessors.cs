using IPA.Utilities;

namespace SongCore.Utilities
{
    internal static class Accessors
    {
        internal static readonly FieldAccessor<CustomPreviewBeatmapLevel, float>.Accessor SongDurationSetter =
            FieldAccessor<CustomPreviewBeatmapLevel, float>.GetAccessor($"<{nameof(CustomPreviewBeatmapLevel.songDuration)}>k__BackingField");
    }
}