using UnityEngine;
using UnityEngine.UI;
namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection, string shortPackName = "") : base(packID, packName, shortPackName, coverImage, customBeatmapLevelCollection)
        {
            coverImage = Sprite.Create(coverImage.texture, coverImage.rect, coverImage.pivot, coverImage.texture.width);
            _coverImage = coverImage;
            if (shortPackName == "")
                _shortPackName = packName;
        }

        public void UpdateLevelCollection(CustomBeatmapLevelCollection newLevelCollection)
        {
            _customBeatmapLevelCollection = newLevelCollection;
        }
    }
}
