using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, UnityEngine.Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection) : base(packID, packName, coverImage, customBeatmapLevelCollection)
        {

        }

        public void UpdateLevelCollection(CustomBeatmapLevelCollection newLevelCollection)
        {
            _customBeatmapLevelCollection = newLevelCollection;
        }
    }
}
