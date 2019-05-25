using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Harmony;
using SongCore.Utilities;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(CustomLevelLoaderSO))]
    [HarmonyPatch("LoadCustomPreviewBeatmapLevelPacksAsync", MethodType.Normal)]
    class LoadingPatches
    {
        static void Prefix(ref CustomLevelLoaderSO.CustomPackFolderInfo[] customPackFolderInfos, CancellationToken cancellationToken)
        {
            var c  = customPackFolderInfos.ToList();
            c.Add(new CustomLevelLoaderSO.CustomPackFolderInfo
            {
                folderName = "CustomWIPLevels",
                packName = "WIP Levels"
            });
            customPackFolderInfos = c.ToArray();


        //    Plugin.LoadWipPack();
        }

        [HarmonyPatch(typeof(LevelPacksViewController))]
        [HarmonyPatch("SetData", MethodType.Normal)]
        class WipPatch2
        {
            static void Postfix(ref IBeatmapLevelPackCollection levelPackCollection)
            {
                Collections.WipLevelPack = levelPackCollection.beatmapLevelPacks.FirstOrDefault(x => x.packName == "WIP Levels") as CustomBeatmapLevelPack;
                if(Collections.WipLevelPack != null)
                    Collections.WipLevelPack.SetField("_coverImage", UI.BasicUI.WIPIcon);
            }
        }
        [HarmonyPatch(typeof(SoloFreePlayFlowCoordinator))]
        [HarmonyPatch("LoadBeatmapLevelPackCollectionAsync", MethodType.Normal)]
        class PackLoadingPatch1
        {
            static void Postfix(ref IBeatmapLevelPackCollection ____levelPackCollection)
            {
                Logging.Log("finished loading");
            }

        }
        [HarmonyPatch(typeof(PartyFreePlayFlowCoordinator))]
        [HarmonyPatch("LoadBeatmapLevelPackCollectionAsync", MethodType.Normal)]
        class PackLoadingPatch2
        {

        }
        [HarmonyPatch(typeof(CustomLevelLoaderSO))]
        [HarmonyPatch("LoadCustomPreviewBeatmapLevelAsync", MethodType.Normal)]
        class SongLoadingPatch
        {
            static void Postfix(string customLevelPath, StandardLevelInfoSaveData standardLevelInfoSaveData, CancellationToken cancellationToken, ref Task<CustomPreviewBeatmapLevel> __result)
            {
                if (__result == null) return;
                CustomPreviewBeatmapLevel level = __result.Result;
     //           Logging.Log(customLevelPath);
                if (level == null) return;
      //          Logging.Log("result: " + level.songName);
                string hash = Utils.GetCustomLevelHash(level);
                if (!Collections._loadedHashes.ContainsKey(hash))
                {
                    List<CustomPreviewBeatmapLevel> value = new List<CustomPreviewBeatmapLevel>();
                    value.Add(level);
                    Collections._loadedHashes.Add(hash, value);
                }
                else
                    Collections._loadedHashes[hash].Add(level);
           //     Logging.Log(Collections._loadedHashes.Count + Collections._loadedHashes.First().Key);
            }
        }
    }
}
