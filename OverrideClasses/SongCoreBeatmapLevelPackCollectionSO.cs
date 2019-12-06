using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongCore.Utilities;
using UnityEngine;
namespace SongCore.OverrideClasses
{
    public class SongCoreBeatmapLevelPackCollectionSO : BeatmapLevelPackCollectionSO
    {
        internal List<CustomBeatmapLevelPack> _customBeatmapLevelPacks = new List<CustomBeatmapLevelPack>();

        public static SongCoreBeatmapLevelPackCollectionSO CreateNew()
        {
            var newCollection = CreateInstance<SongCoreBeatmapLevelPackCollectionSO>();

            newCollection._allBeatmapLevelPacks = new IBeatmapLevelPack[] {};


            newCollection.UpdateArray();
            return newCollection;
        }


        public void AddLevelPack(CustomBeatmapLevelPack pack)
        {
            _customBeatmapLevelPacks.Add(pack);
            UpdateArray();
        }

        private void UpdateArray()
        {
            var packs = _allBeatmapLevelPacks.ToList();
            foreach (var c in _customBeatmapLevelPacks)
                if (!packs.Contains(c))
                    packs.Add(c);
            _allBeatmapLevelPacks = packs.ToArray();
        }
    
}
}
