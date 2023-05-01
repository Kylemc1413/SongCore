using IPA.Utilities;
using UnityEngine;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
        private static readonly FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.Accessor BeatmapLevelCollectionSetter =
            FieldAccessor<CustomBeatmapLevelPack, IBeatmapLevelCollection>.GetAccessor($"<{nameof(beatmapLevelCollection)}>k__BackingField");

        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection, string shortPackName = "")
            : base(packID, packName, shortPackName == string.Empty ? packName : shortPackName, Sprite.Create(coverImage.texture, coverImage.rect, coverImage.pivot, coverImage.texture.width), coverImage, customBeatmapLevelCollection)
        {
        }

        public void UpdateLevelCollection(CustomBeatmapLevelCollection newLevelCollection)
        {
            var that = (CustomBeatmapLevelPack) this;
            BeatmapLevelCollectionSetter(ref that) = newLevelCollection;
        }
    }
}