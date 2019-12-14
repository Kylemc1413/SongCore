using Harmony;
using SongCore.Utilities;
using System;
using UnityEngine;
namespace SongCore.HarmonyPatches
{


    [HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO), "Init", new Type[] {typeof(IDifficultyBeatmap) , typeof(OverrideEnvironmentSettings) ,typeof(ColorScheme),
            typeof(GameplayModifiers) , typeof(PlayerSpecificSettings) , typeof(PracticeSettings) , typeof(string) , typeof(bool)})]
    [HarmonyPatch("Init", MethodType.Normal)]
    class SceneTransitionPatch
    {
        private static void Prefix(IDifficultyBeatmap difficultyBeatmap)
        {
            Data.ExtraSongData.DifficultyData songData = Collections.RetrieveDifficultyData(difficultyBeatmap);
            if (songData == null) return;
            if (songData._colorLeft != null || songData._colorRight != null || songData._envColorLeft != null || songData._envColorRight != null || songData._obstacleColor != null)
            {
                if (Plugin.customSongColors)
                {
                    Logging.logger.Info("Custom Song Colors On");
                    CustomSongColorsPatch.overrideMapData = songData;
                }
                else
                    CustomSongColorsPatch.overrideMapData = null;

            }
        }

    }



    [HarmonyPatch(typeof(ColorManager))]
    [HarmonyPatch("Start", MethodType.Normal)]
    class CustomSongColorsPatch
    {
        internal static Data.ExtraSongData.DifficultyData overrideMapData;
        private static void Prefix(ref ColorScheme ____colorScheme)
        {
            if (overrideMapData != null)
            {
                Logging.Log("Overriding Color Scheme with Map Colors");
                Color saberLeft = overrideMapData._colorLeft == null ? ____colorScheme.saberAColor : Utils.ColorFromMapColor(overrideMapData._colorLeft);
                Color saberRight = overrideMapData._colorRight == null ? ____colorScheme.saberBColor : Utils.ColorFromMapColor(overrideMapData._colorRight);
                Color envLeft = overrideMapData._envColorLeft == null ? overrideMapData._colorLeft == null ? ____colorScheme.environmentColor0 : Utils.ColorFromMapColor(overrideMapData._colorLeft) : Utils.ColorFromMapColor(overrideMapData._envColorLeft);
                Color envRight = overrideMapData._envColorRight == null ? overrideMapData._colorRight == null ? ____colorScheme.environmentColor1 : Utils.ColorFromMapColor(overrideMapData._colorRight) : Utils.ColorFromMapColor(overrideMapData._envColorRight);
                Color obstacle = overrideMapData._obstacleColor == null ? ____colorScheme.obstaclesColor : Utils.ColorFromMapColor(overrideMapData._obstacleColor);
                ColorScheme mapColorScheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft, envRight, obstacle);
                ____colorScheme = mapColorScheme;
                overrideMapData = null;
            }

        }



    }
}
