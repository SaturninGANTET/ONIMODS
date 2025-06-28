using System;
using UnityEngine;

namespace TransitTube_Overlay_Mod
{
    public class TubeOverlayIcon : KMonoBehaviour
    {
        private GameObject iconGO;


        public void SetupInput()
        {
            Setup(Constants.AssetsName.TransitTubeInput);
        }

        public void SetupOutput()
        {
            Setup(Constants.AssetsName.TransitTubeOutput);
        }
        private void Setup(String spriteName)
        {
            var overlaySprite = Assets.GetSprite(spriteName);

            if (iconGO == null)
            {
                iconGO = new GameObject("CustomOverlayIcon");
                var sr = iconGO.AddComponent<SpriteRenderer>();
                sr.sprite = overlaySprite;
                sr.sortingOrder = 200; // Keep above building

                iconGO.layer = LayerMask.NameToLayer("MaskedOverlay");
                iconGO.transform.SetParent(transform);
                iconGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                iconGO.transform.localScale = Vector3.one * 0.25f;
                iconGO.SetActive(false);
            }
        }

        public void SetVisible(bool visible)
        {
            if (iconGO != null)
                iconGO.SetActive(visible);
        }
    }
}
