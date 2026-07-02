using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsPlayerVisualRig : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private GameObject twoDVisualRoot;
        [SerializeField] private GameObject twoPointFiveDVisualRoot;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.08f, 0.08f);
        [SerializeField] private Color invertedColor = Color.white;

        private bool inBlackRegion;
        private PetsPerspectiveMode currentMode;

        public void Configure(GameObject twoDVisual, GameObject twoPointFiveDVisual, Renderer[] renderers, Color normal, Color inverted)
        {
            twoDVisualRoot = twoDVisual;
            twoPointFiveDVisualRoot = twoPointFiveDVisual;
            visualRenderers = renderers;
            normalColor = normal;
            invertedColor = inverted;
            ApplyMode();
            ApplyColor();
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
            ApplyMode();
            ApplyColor();
        }

        public void SetBlackRegionState(bool inside)
        {
            inBlackRegion = inside && currentMode == PetsPerspectiveMode.TwoD;
            ApplyColor();
        }

        private void ApplyMode()
        {
            if (twoDVisualRoot != null)
            {
                twoDVisualRoot.SetActive(currentMode == PetsPerspectiveMode.TwoD);
            }

            if (twoPointFiveDVisualRoot != null)
            {
                twoPointFiveDVisualRoot.SetActive(currentMode == PetsPerspectiveMode.TwoPointFiveD);
            }
        }

        private void ApplyColor()
        {
            if (visualRenderers == null)
            {
                return;
            }

            Color target = inBlackRegion ? invertedColor : normalColor;
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Renderer item = visualRenderers[i];
                if (item != null)
                {
                    item.material.color = target;
                }
            }
        }
    }
}
