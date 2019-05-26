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

        public static SongCoreBeatmapLevelPackCollectionSO ReplaceOriginal(BeatmapLevelPackCollectionSO original)
        {
            var newCollection = CreateInstance<SongCoreBeatmapLevelPackCollectionSO>();
            //  newCollection._allBeatmapLevelPacks.AddRange((BeatmapLevelPackSO[])original.GetField("_beatmapLevelPacks"));
            //Figure out how to properly add the preview song packs
            List<IBeatmapLevelPack> levelPacks = new List<IBeatmapLevelPack>();
            levelPacks.AddRange((BeatmapLevelPackSO[])original.GetField("_beatmapLevelPacks"));
            levelPacks.AddRange((PreviewBeatmapLevelPackSO[])original.GetField("_previewBeatmapLevelPack"));
            newCollection._allBeatmapLevelPacks = levelPacks.ToArray();


            newCollection.UpdateArray();
            newCollection.ReplaceReferences();
            return newCollection;
        }

        public void ReplaceReferences()
        {

            var soloFreePlay = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault();
            if (soloFreePlay != null)
            {
                soloFreePlay.SetPrivateField("_levelPackCollection", this);
            }

            var partyFreePlay = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().FirstOrDefault();
            if (partyFreePlay != null)
            {
                partyFreePlay.SetPrivateField("_levelPackCollection", this);
            }

        }

        public void AddLevelPack(CustomBeatmapLevelPack pack)
        {
            _customBeatmapLevelPacks.Add(pack);
            UpdateArray();
            ReplaceReferences();
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
