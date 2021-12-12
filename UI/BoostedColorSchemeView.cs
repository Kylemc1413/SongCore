using UnityEngine;
using UnityEngine.UI;

namespace SongCore.UI
{
    /*
     * An extension of the base game color scheme view
     * Now with boost colors
     */
    public class BoostedColorSchemeView : ColorSchemeView
    {
        protected Image _environment0BoostColorImage;
        protected Image _environment1BoostColorImage;

        public void Setup()
        {
            _environment0BoostColorImage = GameObject.Instantiate(_environment0ColorImage, transform);
            _environment0BoostColorImage.name = "BoostedEnvironmentColor0";
            _environment1BoostColorImage = GameObject.Instantiate(_environment1ColorImage, transform);
            _environment1BoostColorImage.name = "BoostedEnvironmentColor1";

            _saberAColorImage.transform.localPosition = new Vector3(-29.25f, 0, 0);
            _saberBColorImage.transform.localPosition = new Vector3(-24.75f, 0, 0);
            _environment0ColorImage.transform.localPosition = new Vector3(-20.25f, 0, 0);
            _environment1ColorImage.transform.localPosition = new Vector3(-15.75f, 0, 0);

            BasicUI.AddHoverHintToObject(_saberAColorImage.gameObject).text = "Left Saber Color";
            BasicUI.AddHoverHintToObject(_saberBColorImage.gameObject).text = "Right Saber Color";
            BasicUI.AddHoverHintToObject(_environment0ColorImage.gameObject).text = "Primary Light Color";
            BasicUI.AddHoverHintToObject(_environment1ColorImage.gameObject).text = "Secondary Light Color";
            BasicUI.AddHoverHintToObject(_environment0BoostColorImage.gameObject).text = "Primary Light Boost Color";
            BasicUI.AddHoverHintToObject(_environment1BoostColorImage.gameObject).text = "Secondary Light Boost Color";
            BasicUI.AddHoverHintToObject(_obstacleColorImage.gameObject).text = "Wall Color";
        }

        public void SetColors(Color saberAColor, Color saberBColor, Color environment0Color, Color environment1Color, Color environment0BoostColor, Color environment1BoostColor, Color obstacleColor)
        {
            base.SetColors(saberAColor, saberBColor, environment0Color, environment1Color, obstacleColor);
            _environment0BoostColorImage.color = environment0BoostColor;
            _environment1BoostColorImage.color = environment1BoostColor;
        }
    }
}
