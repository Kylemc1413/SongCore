using Harmony;
//using CustomUI.BeatSaber;
using SongCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("RefreshContent", MethodType.Normal)]

    public class StandardLevelDetailViewRefreshContent
    {
        public static Dictionary<string, OverrideLabels> levelLabels = new Dictionary<string, OverrideLabels>();
        public class OverrideLabels
        {
            internal string EasyOverride = "";
            internal string NormalOverride = "";
            internal string HardOverride = "";
            internal string ExpertOverride = "";
            internal string ExpertPlusOverride = "";
        }

        public static OverrideLabels currentLabels = new OverrideLabels();

        internal static void SetCurrentLabels(OverrideLabels labels)
        {
            currentLabels.EasyOverride = labels.EasyOverride;
            currentLabels.NormalOverride = labels.NormalOverride;
            currentLabels.HardOverride = labels.HardOverride;
            currentLabels.ExpertOverride = labels.ExpertOverride;
            currentLabels.ExpertPlusOverride = labels.ExpertPlusOverride;
        }

        internal static void clearOverrideLabels()
        {
            currentLabels.EasyOverride = "";
            currentLabels.NormalOverride = "";
            currentLabels.HardOverride = "";
            currentLabels.ExpertOverride = "";
            currentLabels.ExpertPlusOverride = "";
        }

        static void Postfix(ref LevelParamsPanel ____levelParamsPanel, ref IDifficultyBeatmap ____selectedDifficultyBeatmap,
            ref PlayerData ____playerData, ref TextMeshProUGUI ____songNameText, ref UnityEngine.UI.Button ____playButton, ref UnityEngine.UI.Button ____practiceButton, ref BeatmapDifficultySegmentedControlController ____beatmapDifficultySegmentedControlController)
        {
            var level = ____selectedDifficultyBeatmap.level is CustomBeatmapLevel ? ____selectedDifficultyBeatmap.level as CustomPreviewBeatmapLevel : null;

            ____playButton.interactable = true;
            ____practiceButton.interactable = true;
            ____playButton.gameObject.GetComponentInChildren<Image>().color = new Color(0, 0.706f, 1.000f, 0.784f);
            ____songNameText.text = "<size=78%>" + ____songNameText.text;
            //    ____songNameText.overflowMode = TextOverflowModes.Overflow;
            //     ____songNameText.enableWordWrapping = false;
            ____songNameText.richText = true;
            if (level != null)
            {
                Data.ExtraSongData songData = Collections.RetrieveExtraSongData(Utilities.Hashing.GetCustomLevelHash(level), level.customLevelPath);

                if (songData == null)
                {
                    RequirementsUI.instance.ButtonGlowColor = "none";
                    RequirementsUI.instance.ButtonInteractable = false;
                    return;
                }
                bool wipFolderSong = false;
                IDifficultyBeatmap selectedDiff = ____selectedDifficultyBeatmap;
                Data.ExtraSongData.DifficultyData diffData = Collections.RetrieveDifficultyData(selectedDiff);
                //songData._difficulties?.FirstOrDefault(x => x._difficulty == selectedDiff.difficulty
                //&& (x._beatmapCharacteristicName == selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicName || x._beatmapCharacteristicName == selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName));
                if (diffData != null)
                {
                    //If no additional information is present
                    if (diffData.additionalDifficultyData._requirements.Count() == 0 && diffData.additionalDifficultyData._suggestions.Count() == 0
                        && diffData.additionalDifficultyData._warnings.Count() == 0 && diffData.additionalDifficultyData._information.Count() == 0
                        && songData.contributors.Count() == 0)
                    {
                        RequirementsUI.instance.ButtonGlowColor = "none";
                        RequirementsUI.instance.ButtonInteractable = false;
                    }
                    else if (diffData.additionalDifficultyData._warnings.Count() == 0)
                    {
                        RequirementsUI.instance.ButtonGlowColor = "#0000FF";
                        RequirementsUI.instance.ButtonInteractable = true;
                    }
                    else if (diffData.additionalDifficultyData._warnings.Count() > 0)
                    {
                        RequirementsUI.instance.ButtonGlowColor = "#FFFF00";
                        RequirementsUI.instance.ButtonInteractable = true;
                        if (diffData.additionalDifficultyData._warnings.Contains("WIP"))
                        {
                            ____playButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                        }

                    }
                }
                if (level.levelID.EndsWith(" WIP"))
                {
                    RequirementsUI.instance.ButtonGlowColor = "#FFFF00";
                    RequirementsUI.instance.ButtonInteractable = true;
                    ____playButton.interactable = false;
                    ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                    wipFolderSong = true;

                }
                if (diffData != null)
                {

                    for (int i = 0; i < diffData.additionalDifficultyData._requirements.Count(); i++)
                    {
                        if (!Collections.capabilities.Contains(diffData.additionalDifficultyData._requirements[i]))
                        {
                            ____playButton.interactable = false;
                            ____practiceButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                            RequirementsUI.instance.ButtonGlowColor = "#FF0000";
                        }
                    }
                }


                if (selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName == "MissingCharacteristic")
                {
                    ____playButton.interactable = false;
                    ____practiceButton.interactable = false;
                    ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                    RequirementsUI.instance.ButtonGlowColor = "#FF0000";
                }

                RequirementsUI.instance.level = level;
                RequirementsUI.instance.songData = songData;
                RequirementsUI.instance.diffData = diffData;
                RequirementsUI.instance.wipFolder = wipFolderSong;


                //Difficulty Label Handling
                levelLabels.Clear();
                string currentCharacteristic = "";
                foreach (Data.ExtraSongData.DifficultyData diffLevel in songData._difficulties)
                {
                    var difficulty = diffLevel._difficulty;
                    string characteristic = diffLevel._beatmapCharacteristicName;
                    if (characteristic == selectedDiff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName)
                        currentCharacteristic = characteristic;
                    if (!levelLabels.ContainsKey(characteristic))
                        levelLabels.Add(characteristic, new OverrideLabels());
                    OverrideLabels charLabels = levelLabels[characteristic];
                    if (!string.IsNullOrWhiteSpace(diffLevel._difficultyLabel))
                    {
                        switch (difficulty)
                        {
                            case BeatmapDifficulty.Easy:
                                charLabels.EasyOverride = diffLevel._difficultyLabel;
                                break;
                            case BeatmapDifficulty.Normal:
                                charLabels.NormalOverride = diffLevel._difficultyLabel;
                                break;
                            case BeatmapDifficulty.Hard:
                                charLabels.HardOverride = diffLevel._difficultyLabel;
                                break;
                            case BeatmapDifficulty.Expert:
                                charLabels.ExpertOverride = diffLevel._difficultyLabel;
                                break;
                            case BeatmapDifficulty.ExpertPlus:
                                charLabels.ExpertPlusOverride = diffLevel._difficultyLabel;
                                break;
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(currentCharacteristic))
                    SetCurrentLabels(levelLabels[currentCharacteristic]);
                else
                    clearOverrideLabels();

                ____beatmapDifficultySegmentedControlController.SetData(____selectedDifficultyBeatmap.parentDifficultyBeatmapSet.difficultyBeatmaps, ____beatmapDifficultySegmentedControlController.selectedDifficulty);
                clearOverrideLabels();




            }

        }

    }
}

