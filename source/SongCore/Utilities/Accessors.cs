using IPA.Utilities;

namespace SongCore.Utilities
{
    internal static class Accessors
    {
        public static readonly FieldAccessor<BeatmapLevel, string>.Accessor LevelIDAccessor =
            FieldAccessor<BeatmapLevel, string>.GetAccessor(nameof(BeatmapLevel.levelID));

        public static readonly FieldAccessor<BeatmapLevel, float>.Accessor SongDurationAccessor =
            FieldAccessor<BeatmapLevel, float>.GetAccessor(nameof(BeatmapLevel.songDuration));

        public static readonly FieldAccessor<BeatmapLevel, float>.Accessor PreviewDurationAccessor =
            FieldAccessor<BeatmapLevel, float>.GetAccessor(nameof(BeatmapLevel.previewDuration));

        public static readonly FieldAccessor<BeatmapLevelPack, BeatmapLevel[]>.Accessor BeatmapLevelsAccessor =
            FieldAccessor<BeatmapLevelPack, BeatmapLevel[]>.GetAccessor(nameof(BeatmapLevelPack.beatmapLevels));

        public static readonly FieldAccessor<SaberManager.InitData, bool>.Accessor OneSaberModeAccessor =
            FieldAccessor<SaberManager.InitData, bool>.GetAccessor(nameof(SaberManager.InitData.oneSaberMode));
    }
}
