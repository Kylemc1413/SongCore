using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomLevelCollection : CustomBeatmapLevelCollection
    {
    //    public readonly List<CustomPreviewBeatmapLevel> _levels = new List<CustomPreviewBeatmapLevel>();
        public SongCoreCustomLevelCollection(CustomPreviewBeatmapLevel[] customPreviewBeatmapLevels) : base(customPreviewBeatmapLevels)
        {
        }

        public void UpdatePreviewLevels(CustomPreviewBeatmapLevel[] levels)
        {
            _customPreviewBeatmapLevels = levels;
        }
    }
}
