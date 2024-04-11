using System;
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;

namespace SongCore.OverrideClasses
{
    public class SongCoreBeatmapLevelsRepository : BeatmapLevelsRepository
    {
        private static readonly FieldAccessor<BeatmapLevelsRepository, BeatmapLevelPack[]>.Accessor BeatmapLevelPacksAccessor =
            FieldAccessor<BeatmapLevelsRepository, BeatmapLevelPack[]>.GetAccessor(nameof(_beatmapLevelPacks));

        private readonly List<BeatmapLevelPack> _customBeatmapLevelPacks = new List<BeatmapLevelPack>();

        private SongCoreBeatmapLevelsRepository(IEnumerable<BeatmapLevelPack> beatmapLevelPacks) : base(beatmapLevelPacks)
        {
        }

        public static SongCoreBeatmapLevelsRepository CreateNew()
        {
            
            return new SongCoreBeatmapLevelsRepository(Array.Empty<BeatmapLevelPack>());
        }

        public void AddLevelPack(BeatmapLevelPack pack)
        {
            if (pack != null && !_customBeatmapLevelPacks.Contains(pack))
            {
                _customBeatmapLevelPacks.Add(pack);
                RefreshCollections();
            }
        }

        public void RemoveLevelPack(BeatmapLevelPack pack)
        {
            if (pack != null && _customBeatmapLevelPacks.Contains(pack))
            {
                _customBeatmapLevelPacks.Remove(pack);
                RefreshCollections();
            }
        }

        public void ClearLevelPacks()
        {
            _customBeatmapLevelPacks.Clear();
        }

        private void RefreshCollections()
        {
            _idToBeatmapLevelPack.Clear();
            _idToBeatmapLevel.Clear();
            _beatmapLevelIdToBeatmapLevelPackId.Clear();

            var that = (BeatmapLevelsRepository)this;
            BeatmapLevelPacksAccessor(ref that) = _customBeatmapLevelPacks.ToArray();
            foreach (BeatmapLevelPack beatmapLevelPack in _beatmapLevelPacks)
            {
                _idToBeatmapLevelPack.Add(beatmapLevelPack.packID, beatmapLevelPack);
                foreach (BeatmapLevel beatmapLevel in beatmapLevelPack.beatmapLevels)
                {
                    _beatmapLevelIdToBeatmapLevelPackId.TryAdd(beatmapLevel.levelID, beatmapLevelPack.packID);
                    _idToBeatmapLevel.TryAdd(beatmapLevel.levelID, beatmapLevel);
                }
            }
        }

    }
}