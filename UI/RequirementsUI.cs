using BeatSaberMarkupLanguage;
using BS_Utils.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SongCore.UI
{
    public class RequirementsUI : PersistentSingleton<RequirementsUI>
    {
        private StandardLevelDetailViewController standardLevel;


        internal static Config ModPrefs = new Config("SongCore/SongCore");
        internal Button infoButton;

        internal Sprite HaveReqIcon;
        internal Sprite MissingReqIcon;
        internal Sprite HaveSuggestionIcon;
        internal Sprite MissingSuggestionIcon;
        internal Sprite WarningIcon;
        internal Sprite InfoIcon;
        internal Sprite MissingCharIcon;
        internal Sprite LightshowIcon;
        internal Sprite ExtraDiffsIcon;
        internal Sprite WIPIcon;
        internal Sprite FolderIcon;

        internal void Setup()
        {
            standardLevel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.requirements.bsml"), standardLevel.gameObject, this);
        }
    }
}
