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
    class WipPatch
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
    }
}
