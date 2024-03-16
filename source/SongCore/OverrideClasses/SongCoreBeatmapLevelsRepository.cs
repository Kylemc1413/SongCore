using System;
using System.Collections.Generic;
using IPA.Utilities;

namespace SongCore.OverrideClasses
{
    public class SongCoreBeatmapLevelsRepository : BeatmapLevelsRepository
    {
        private static readonly FieldAccessor<BeatmapLevelsRepository, IReadOnlyList<BeatmapLevelPack>>.Accessor BeatmapLevelPacksAccessor =
            FieldAccessor<BeatmapLevelsRepository, IReadOnlyList<BeatmapLevelPack>>.GetAccessor(nameof(_beatmapLevelPacks));

        private readonly List<BeatmapLevelPack> _customBeatmapLevelPacks = new List<BeatmapLevelPack>();

        private SongCoreBeatmapLevelsRepository(IReadOnlyList<BeatmapLevelPack> beatmapLevelPacks) : base(beatmapLevelPacks)
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
            var that = (BeatmapLevelsRepository)this;
            BeatmapLevelPacksAccessor(ref that) = _customBeatmapLevelPacks.ToArray();

            _idToBeatmapLevelPack.Clear();
            _beatmapLevelIdToBeatmapLevelPackId.Clear();
            _idToBeatmapLevel.Clear();

            foreach (BeatmapLevelPack beatmapLevelPack in beatmapLevelPacks)
            {
                _idToBeatmapLevelPack.Add(beatmapLevelPack.packID, beatmapLevelPack);
                BeatmapLevel[] beatmapLevels = beatmapLevelPack.beatmapLevels;
                foreach (BeatmapLevel beatmapLevel in beatmapLevels)
                {
                    _beatmapLevelIdToBeatmapLevelPackId.Add(beatmapLevel.levelID, beatmapLevelPack.packID);
                    _idToBeatmapLevel.Add(beatmapLevel.levelID, beatmapLevel);
                }
            }
        }

    }
}