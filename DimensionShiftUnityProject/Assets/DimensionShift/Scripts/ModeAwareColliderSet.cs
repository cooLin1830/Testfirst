using UnityEngine;

namespace DimensionShift
{
    public sealed class ModeAwareColliderSet : DimensionListenerBehaviour
    {
        [SerializeField] private Collider[] twoDOnlyColliders;
        [SerializeField] private Collider[] threeDOnlyColliders;
        [SerializeField] private Collider[] alwaysOnColliders;
        [SerializeField] private Renderer[] twoDOnlyRenderers;
        [SerializeField] private Renderer[] threeDOnlyRenderers;
        [SerializeField] private Renderer[] alwaysOnRenderers;

        public override void SetDimensionMode(DimensionMode mode)
        {
            SetColliders(twoDOnlyColliders, mode == DimensionMode.TwoD);
            SetColliders(threeDOnlyColliders, mode == DimensionMode.ThreeD);
            SetColliders(alwaysOnColliders, true);

            SetRenderers(twoDOnlyRenderers, mode == DimensionMode.TwoD);
            SetRenderers(threeDOnlyRenderers, mode == DimensionMode.ThreeD);
            SetRenderers(alwaysOnRenderers, true);
        }

        private static void SetColliders(Collider[] colliders, bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }

        private static void SetRenderers(Renderer[] renderers, bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }
    }
}
