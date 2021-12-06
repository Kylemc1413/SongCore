using BeatSaberMarkupLanguage.Attributes;

namespace SongCore.UI
{
    public class SCSettings : PersistentSingleton<SCSettings>
    {
        [UIValue("colors")]
        public bool Colors
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            set
            {
                Plugin.CustomSongColors = value;
                BasicUI.ModPrefs.SetBool("SongCore", "customSongColors", value);
            }
        }

        [UIValue("platforms")]
        public bool Platforms
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            set
            {
                Plugin.CustomSongPlatforms = value;
                BasicUI.ModPrefs.SetBool("SongCore", "customSongPlatforms", value);
            }
        }

        [UIValue("diffLabels")]
        public bool DiffLabels
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "displayDiffLabels", true, true);
            set
            {
                Plugin.DisplayDiffLabels = value;
                BasicUI.ModPrefs.SetBool("SongCore", "displayDiffLabels", value);
            }
        }

        [UIValue("longPreviews")]
        public bool LongPreviews
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "forceLongPreviews", false, true);
            set
            {
                Plugin.ForceLongPreviews = value;
                BasicUI.ModPrefs.SetBool("SongCore", "forceLongPreviews", value);
            }
        }
    }
}