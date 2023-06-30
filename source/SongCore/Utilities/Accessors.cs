using IPA.Utilities;

namespace SongCore.Utilities
{
    internal static class Accessors
    {
        internal static readonly FieldAccessor<CustomPreviewBeatmapLevel, float>.Accessor SongDurationAccessor =
            FieldAccessor<CustomPreviewBeatmapLevel, float>.GetAccessor(ReflectionUtil.ToCompilerGeneratedBackingField(nameof(CustomPreviewBeatmapLevel.songDuration)));

        internal static readonly FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.Accessor BeatmapLevelCollectionAccessor =
            FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.GetAccessor(ReflectionUtil.ToCompilerGeneratedBackingField(nameof(CustomBeatmapLevelPack.beatmapLevelCollection)));
    }
}