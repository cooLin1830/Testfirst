using UnityEngine;

namespace DimensionShift
{
    public sealed class DimensionHazard : DimensionListenerBehaviour
    {
        [SerializeField] private bool dangerousIn2D = true;
        [SerializeField] private bool dangerousIn3D = false;
        [SerializeField] private Renderer hazardRenderer;
        [SerializeField] private Color dangerousColor = new Color(0.02f, 0.02f, 0.02f);
        [SerializeField] private Color safeColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);

        private bool dangerous;

        private void Reset()
        {
            hazardRenderer = GetComponentInChildren<Renderer>();
        }

        private void Awake()
        {
            if (hazardRenderer == null)
            {
                hazardRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!dangerous)
            {
                return;
            }

            DimensionPlayerController player = other.GetComponentInParent<DimensionPlayerController>();
            if (player != null)
            {
                player.ResetToSpawn();
            }
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            dangerous = mode == DimensionMode.TwoD ? dangerousIn2D : dangerousIn3D;
            if (hazardRenderer != null)
            {
                hazardRenderer.material.color = dangerous ? dangerousColor : safeColor;
            }
        }
    }
}
