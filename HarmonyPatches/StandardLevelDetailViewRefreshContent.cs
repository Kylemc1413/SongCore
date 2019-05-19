using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CustomUI.BeatSaber;
using MenuUI = SongCore.UI.BasicUI;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("RefreshContent", MethodType.Normal)]

    public class StandardLevelDetailViewRefreshContent
    {
        internal static string EasyOverride = "";
        internal static string NormalOverride = "";
        internal static string HardOverride = "";
        internal static string ExpertOverride = "";
        internal static string ExpertPlusOverride = "";


        internal static void clearOverrideLabels()
        {
            EasyOverride = "";
            NormalOverride = "";
            HardOverride = "";
            ExpertOverride = "";
            ExpertPlusOverride = "";
        }

        static void Postfix(ref LevelParamsPanel ____levelParamsPanel, ref IDifficultyBeatmap ____selectedDifficultyBeatmap,
            ref IPlayer ____player, ref TextMeshProUGUI ____songNameText, ref UnityEngine.UI.Button ____playButton, ref UnityEngine.UI.Button ____practiceButton, ref BeatmapDifficultySegmentedControlController ____beatmapDifficultySegmentedControlController)
        {
            var level = ____selectedDifficultyBeatmap.level is CustomBeatmapLevel ? ____selectedDifficultyBeatmap.level as CustomPreviewBeatmapLevel : null;

            ____playButton.interactable = true;
            ____practiceButton.interactable = true;
            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = new Color(0, 0.706f, 1.000f, 0.784f);
            ____songNameText.text = "<size=78%>" + ____songNameText.text;
            //    ____songNameText.overflowMode = TextOverflowModes.Overflow;
            //     ____songNameText.enableWordWrapping = false;
            ____songNameText.richText = true;
            if (level != null)
            {
                Data.ExtraSongData songData = Collections.RetrieveExtraSongData(Utilities.Utils.GetCustomLevelIdentifier(level), level.customLevelPath);

                if (MenuUI.infoButton == null)
                {
                    Console.WriteLine("Creating Info Button");

                    MenuUI.infoButton = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PlayButton")), (RectTransform)____levelParamsPanel.transform.parent, false);
                    MenuUI.infoButton.SetButtonText("?");
                    (MenuUI.infoButton.transform as RectTransform).anchorMax = new Vector2(1, 1);
                    (MenuUI.infoButton.transform as RectTransform).anchorMin = new Vector2(1, 1);
                    (MenuUI.infoButton.transform as RectTransform).pivot = new Vector2(1, 1);
                    (MenuUI.infoButton.transform as RectTransform).anchoredPosition = new Vector2(-1f, -1f);

                    //   SongLoader.infoButton.GetComponentInChildren<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
                    //          (SongLoader.infoButton.transform as RectTransform).sizeDelta = new Vector2(0.11f, 0.1f);
                    MenuUI.infoButton.transform.localScale *= 0.5f;
                    
                }
                if(songData == null)
                {
                    MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                    MenuUI.infoButton.interactable = false;
                    return;
                }
                bool wipFolderSong = false;
                IDifficultyBeatmap selectedDiff = ____selectedDifficultyBeatmap;
                Data.ExtraSongData.DifficultyData diffData = songData.difficulties.FirstOrDefault(x => x.difficulty == selectedDiff.difficulty
                && (x.beatmapCharacteristicName == selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName || x.beatmapCharacteristicName == selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName));
                if(diffData != null)
                {
                    //If no additional information is present
                    if (diffData.additionalDifficultyData.requirements.Count() == 0 && diffData.additionalDifficultyData.requirements.Count() == 0
                        && diffData.additionalDifficultyData.requirements.Count() == 0 && diffData.additionalDifficultyData.requirements.Count() == 0
                        && songData.contributors.Count() == 0)
                    {
                        MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                        MenuUI.infoButton.interactable = false;
                    }
                    else if (diffData.additionalDifficultyData.warnings.Count() == 0)
                    {
                        MenuUI.infoButton.interactable = true;
                        MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.blue;
                    }
                    else if (diffData.additionalDifficultyData.warnings.Count() > 0)
                    {
                        MenuUI.infoButton.interactable = true;
                        MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                        if (diffData.additionalDifficultyData.warnings.Contains("WIP"))
                        {
                            ____playButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                        }

                    }
                }
             
                if (songData.songPath.Contains("WIP Songs"))
                {
                    MenuUI.infoButton.interactable = true;
                    MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                    ____playButton.interactable = false;
                    ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                    wipFolderSong = true;

                }
                if(diffData != null)
                {

                    for (int i = 0; i < diffData.additionalDifficultyData.requirements.Count(); i++)
                    {
                        if (!Collections.capabilities.Contains(diffData.additionalDifficultyData.requirements[i]))
                        {
                            ____playButton.interactable = false;
                            ____practiceButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                            MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                        }
                    }
                }


                if(selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicName == "Missing Characteristic")
                {
                    ____playButton.interactable = false;
                    ____practiceButton.interactable = false;
                    ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                    MenuUI.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                }

                MenuUI.infoButton.onClick.RemoveAllListeners();
                MenuUI.infoButton.onClick.AddListener(delegate ()
                {
                    //Console.WriteLine("Click");
                    MenuUI.showSongRequirements(songData, diffData, wipFolderSong);
                });


                //Difficulty Label Handling
                bool overrideLabels = false;
                foreach (Data.ExtraSongData.DifficultyData diffLevel in songData.difficulties)
                {
                    var difficulty = diffLevel.difficulty;
                    if (!string.IsNullOrWhiteSpace(diffLevel.difficultyLabel))
                    {
                        //   Console.WriteLine("Diff: " + difficulty + "   Label: " + diffLevel.difficultyLabel);
                        overrideLabels = true;
                        switch (difficulty)
                        {
                            case BeatmapDifficulty.Easy:
                                EasyOverride = diffLevel.difficultyLabel;
                                break;
                            case BeatmapDifficulty.Normal:
                                NormalOverride = diffLevel.difficultyLabel;
                                break;
                            case BeatmapDifficulty.Hard:
                                HardOverride = diffLevel.difficultyLabel;
                                break;
                            case BeatmapDifficulty.Expert:
                                ExpertOverride = diffLevel.difficultyLabel;
                                break;
                            case BeatmapDifficulty.ExpertPlus:
                                ExpertPlusOverride = diffLevel.difficultyLabel;
                                break;
                        }
                    }
                }
                if (overrideLabels)
                {
                    //  Console.WriteLine("Overriding");
                    ____beatmapDifficultySegmentedControlController.SetData(____selectedDifficultyBeatmap.parentDifficultyBeatmapSet.difficultyBeatmaps, ____beatmapDifficultySegmentedControlController.selectedDifficulty);
                    clearOverrideLabels();
                }




            }

            /*
            if (level != null)
            {

                var customLevel = level as CustomLevel;

                CustomLevel.CustomDifficultyBeatmap beatmap = ____selectedDifficultyBeatmap as CustomLevel.CustomDifficultyBeatmap;

                if (SongLoader.infoButton == null)
                {
                    Console.WriteLine("Creating Info Button");

                    SongLoader.infoButton = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PlayButton")), (RectTransform)____levelParamsPanel.transform.parent, false);
                    SongLoader.infoButton.SetButtonText("?");
                    (SongLoader.infoButton.transform as RectTransform).anchorMax = new Vector2(1, 1);
                    (SongLoader.infoButton.transform as RectTransform).anchorMin = new Vector2(1, 1);
                    (SongLoader.infoButton.transform as RectTransform).pivot = new Vector2(1, 1);
                    (SongLoader.infoButton.transform as RectTransform).anchoredPosition = new Vector2(-1f, -1f);

                    //   SongLoader.infoButton.GetComponentInChildren<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
                    //          (SongLoader.infoButton.transform as RectTransform).sizeDelta = new Vector2(0.11f, 0.1f);
                    SongLoader.infoButton.transform.localScale *= 0.5f;

                }
                */

            /*
                if (beatmap != null)
                {
                    SongLoader.infoButton.onClick.RemoveAllListeners();
                    SongLoader.infoButton.onClick.AddListener(delegate ()
                    {
                        //Console.WriteLine("Click");
                        if (beatmap != null)
                            SongLoader.showSongRequirements(beatmap, customLevel.customSongInfo);
                    });
                    if (beatmap.requirements.Count == 0 && beatmap.suggestions.Count == 0 && beatmap.warnings.Count == 0 &&
                        customLevel?.customSongInfo?.mappers?.Length == 0 && customLevel?.customSongInfo?.lighters?.Length == 0 && beatmap.information.Count == 0 && customLevel.customSongInfo.contributors.Length == 0)
                    {
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                        SongLoader.infoButton.interactable = false;
                    }
                    else if (beatmap.warnings.Count == 0)
                    {
                        SongLoader.infoButton.interactable = true;
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.blue;
                    }
                    else if (beatmap.warnings.Count > 0)
                    {
                        SongLoader.infoButton.interactable = true;
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                        if (beatmap.warnings.Contains("WIP"))
                        {
                            ____playButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                        }
                    }


                    SongLoader.currentRequirements = beatmap.requirements;
                    SongLoader.currentSuggestions = beatmap.suggestions;

                    for (int i = 0; i < beatmap.requirements.Count; i++)
                    {
                        if (!SongLoader.capabilities.Contains(beatmap.requirements[i]))
                        {
                            ____playButton.interactable = false;
                            ____practiceButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                            SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                        }
                    }
                    */
                    //Difficulty Label Handling
            /*
                    foreach (CustomSongInfo.DifficultyLevel diffLevel in customLevel.customSongInfo.difficultyLevels)
                    {
                        var difficulty = diffLevel.difficulty.ToEnum(BeatmapDifficulty.Normal);
                        if (!string.IsNullOrWhiteSpace(diffLevel.difficultyLabel))
                        {
                         //   Console.WriteLine("Diff: " + difficulty + "   Label: " + diffLevel.difficultyLabel);
                            overrideLabels = true;
                            switch (difficulty)
                            {
                                case BeatmapDifficulty.Easy:
                                    EasyOverride = diffLevel.difficultyLabel;
                                    break;
                                case BeatmapDifficulty.Normal:
                                    NormalOverride = diffLevel.difficultyLabel;
                                    break;
                                case BeatmapDifficulty.Hard:
                                    HardOverride = diffLevel.difficultyLabel;
                                    break;
                                case BeatmapDifficulty.Expert:
                                    ExpertOverride = diffLevel.difficultyLabel;
                                    break;
                                case BeatmapDifficulty.ExpertPlus:
                                    ExpertPlusOverride = diffLevel.difficultyLabel;
                                    break;
                            }
                        }
                    }
                        if (overrideLabels)
                    {
                      //  Console.WriteLine("Overriding");
                        ____beatmapDifficultySegmentedControlController.SetData(____selectedDifficultyBeatmap.parentDifficultyBeatmapSet.difficultyBeatmaps, ____beatmapDifficultySegmentedControlController.selectedDifficulty);
                        clearOverrideLabels();
                    }
                }
                else
                {
                    SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                    SongLoader.infoButton.interactable = false;
                }

              
                



            }
            */

        }

    }
}

