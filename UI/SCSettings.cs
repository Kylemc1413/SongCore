using BeatSaberMarkupLanguage.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BS_Utils.Utilities;

namespace SongCore.UI
{

    public class SCSettings : PersistentSingleton<SCSettings>
    {
        [UIValue("colors")]
        public bool Colors
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            set
            {
                Plugin.customSongColors = value;
                BasicUI.ModPrefs.SetBool("SongCore", "customSongColors", value);
            }
        }

        [UIValue("platforms")]
        public bool Platforms
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            set
            {
                Plugin.customSongPlatforms = value;
                BasicUI.ModPrefs.SetBool("SongCore", "customSongPlatforms", value);
            }
        }
    }
}
