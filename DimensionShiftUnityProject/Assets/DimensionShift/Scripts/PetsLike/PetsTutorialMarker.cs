using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsTutorialMarker : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private PetsLevelRuntime level;
        [SerializeField] private PetsGridCoord coord;
        [SerializeField] private TextMesh label;
        [SerializeField] private float twoDVerticalOffset = 0.34f;
        [SerializeField] private float twoPointFiveDHeight = 0.16f;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord, string text, Color color, float size)
        {
            level = levelRuntime;
            coord = gridCoord;
            label = gameObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = size;
            label.fontSize = 72;
            label.color = color;
            SetPerspectiveMode(PetsModeManager.Instance != null ? PetsModeManager.Instance.CurrentMode : PetsPerspectiveMode.TwoD);
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            if (level == null)
            {
                return;
            }

            bool visible = mode == PetsPerspectiveMode.TwoD;
            if (label != null)
            {
                Renderer labelRenderer = label.GetComponent<Renderer>();
                if (labelRenderer != null)
                {
                    labelRenderer.enabled = visible;
                }
            }

            if (!visible)
            {
                return;
            }

            if (mode == PetsPerspectiveMode.TwoD)
            {
                transform.position = level.GridToTwoDWorld(coord, -0.48f) + Vector3.up * twoDVerticalOffset;
                transform.rotation = Quaternion.identity;
            }
            else
            {
                transform.position = level.GridToTopDownWorld(coord, twoPointFiveDHeight);
                transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            }
        }
    }
}
