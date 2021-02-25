using HarmonyLib;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace SongCore.HarmonyPatches
{


    [HarmonyPatch]
    class SceneTransitionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(StandardLevelScenesTransitionSetupDataSO).GetMethod("Init", new Type[] {typeof(string), typeof(IDifficultyBeatmap) , typeof(OverrideEnvironmentSettings) ,typeof(ColorScheme),
                typeof(GameplayModifiers) , typeof(PlayerSpecificSettings) , typeof(PracticeSettings) , typeof(string) , typeof(bool)});
            
            yield return typeof(MultiplayerLevelScenesTransitionSetupDataSO).GetMethod("Init", new Type[] {typeof(string), typeof(IPreviewBeatmapLevel) , typeof(BeatmapDifficulty) ,typeof(BeatmapCharacteristicSO),
                typeof(IDifficultyBeatmap) , typeof(ColorScheme) , typeof(GameplayModifiers) , typeof(PlayerSpecificSettings) , typeof(PracticeSettings) , typeof(bool)});
        }

        private static void Prefix(IDifficultyBeatmap difficultyBeatmap, ref ColorScheme overrideColorScheme)
        {
            EnvironmentInfoSO environmentInfoSO = difficultyBeatmap.GetEnvironmentInfo();
            Data.ExtraSongData.DifficultyData songData = Collections.RetrieveDifficultyData(difficultyBeatmap);
            ColorScheme fallbackScheme = overrideColorScheme ?? new ColorScheme(environmentInfoSO.colorScheme);
            if (songData == null) return;
            if (songData._colorLeft != null || songData._colorRight != null || songData._envColorLeft != null || songData._envColorRight != null || songData._obstacleColor != null)
            {
                if (Plugin.customSongColors)
                {
                    Logging.logger.Info("Custom Song Colors On");
                    Color saberLeft = songData._colorLeft == null ? fallbackScheme.saberAColor : Utils.ColorFromMapColor(songData._colorLeft);
                    Color saberRight = songData._colorRight == null ? fallbackScheme.saberBColor : Utils.ColorFromMapColor(songData._colorRight);
                    Color envLeft = songData._envColorLeft == null ? songData._colorLeft == null ? fallbackScheme.environmentColor0 : Utils.ColorFromMapColor(songData._colorLeft) : Utils.ColorFromMapColor(songData._envColorLeft);
                    Color envRight = songData._envColorRight == null ? songData._colorRight == null ? fallbackScheme.environmentColor1 : Utils.ColorFromMapColor(songData._colorRight) : Utils.ColorFromMapColor(songData._envColorRight);
                    Color envLeftBoost = songData._envColorLeftBoost == null ? envLeft : Utils.ColorFromMapColor(songData._envColorLeftBoost);
                    Color envRightBoost = songData._envColorRightBoost == null ? envRight : Utils.ColorFromMapColor(songData._envColorRightBoost);
                    Color obstacle = songData._obstacleColor == null ? fallbackScheme.obstaclesColor : Utils.ColorFromMapColor(songData._obstacleColor);
                    ColorScheme mapColorScheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft, envRight, true, envLeftBoost, envRightBoost, obstacle);
                    overrideColorScheme = mapColorScheme;
                }


            }
        }

    }
    

    /*
    [HarmonyPatch(typeof(ColorManager))]
    [HarmonyPatch("Awake", MethodType.Normal)]
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
    */
}
