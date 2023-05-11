using SongCore.Utilities;
using UnityEngine;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection, string shortPackName = "")
            : base(packID, packName, shortPackName == string.Empty ? packName : shortPackName, Sprite.Create(coverImage.texture, coverImage.rect, coverImage.pivot, coverImage.texture.width), coverImage, customBeatmapLevelCollection)
        {
        }

        public void UpdateLevelCollection(CustomBeatmapLevelCollection newLevelCollection)
        {
            var that = (CustomBeatmapLevelPack) this;
            Accessors.BeatmapLevelCollectionAccessor(ref that) = newLevelCollection;
        }
    }
}