using System.Linq;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : BeatmapLevelPack
    {
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite coverImage, BeatmapLevel[] beatmapLevels, string shortPackName = "")
            : base(packID, packName, shortPackName.Length == 0 ? packName : shortPackName,  coverImage == Loader.defaultCoverImage ? coverImage : Sprite.Create(coverImage.texture, coverImage.rect, coverImage.pivot, coverImage.texture.width), coverImage, PackBuyOption.Default, beatmapLevels, PlayerSensitivityFlag.Safe)
        {
        }

        public void UpdateBeatmapLevels(BeatmapLevel[] beatmapLevels)
        {
            var that = (BeatmapLevelPack)this;
            Accessors.AllBeatmapLevelsAccessor(ref that) = beatmapLevels.Concat(_additionalBeatmapLevels).ToList();
        }
    }
}