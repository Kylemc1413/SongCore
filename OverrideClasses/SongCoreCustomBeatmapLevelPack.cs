namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, UnityEngine.Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection, string shortPackName = "") : base(packID, packName, shortPackName, coverImage, customBeatmapLevelCollection)
        {
            if (shortPackName == "")
                _shortPackName = packName;
        }

        public void UpdateLevelCollection(CustomBeatmapLevelCollection newLevelCollection)
        {
            _customBeatmapLevelCollection = newLevelCollection;
        }
    }
}
