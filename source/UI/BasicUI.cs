using System.Linq;
using HMUI;
using UnityEngine;
using IPA.Utilities;

namespace SongCore.UI
{
    internal static class BasicUI
    {
        private static HoverHintController? hoverHintController = null;

        internal static Sprite? MissingCharIcon;
        internal static Sprite? LightshowIcon;
        internal static Sprite? ExtraDiffsIcon;
        internal static Sprite? WIPIcon;
        internal static Sprite? FolderIcon;

        internal static void GetIcons()
        {
            if (!MissingCharIcon)
            {
                MissingCharIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.MissingChar.png")!;
            }

            if (!LightshowIcon)
            {
                LightshowIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Lightshow.png")!;
            }

            if (!ExtraDiffsIcon)
            {
                ExtraDiffsIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.ExtraDiffsIcon.png")!;
            }

            if (!WIPIcon)
            {
                WIPIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.squek.png")!;
            }

            if (!FolderIcon)
            {
                FolderIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.FolderIcon.png")!;
            }
        }
    }
}