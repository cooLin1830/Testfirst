using UnityEngine;

namespace DimensionShift
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DimensionPushableBox : DimensionListenerBehaviour
    {
        [SerializeField] private float twoDPlaneZ = 0f;
        [SerializeField] private Color twoDColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color threeDColor = new Color(0.9f, 0.48f, 0.23f);

        private Rigidbody body;
        private Renderer boxRenderer;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            boxRenderer = GetComponentInChildren<Renderer>();
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.constraints = RigidbodyConstraints.FreezeRotation;

            if (mode == DimensionMode.TwoD)
            {
                body.constraints |= RigidbodyConstraints.FreezePositionZ;
                Vector3 position = body.position;
                position.z = twoDPlaneZ;
                body.position = position;

                Vector3 velocity = body.velocity;
                velocity.z = 0f;
                body.velocity = velocity;
            }

            if (boxRenderer != null)
            {
                boxRenderer.material.color = mode == DimensionMode.TwoD ? twoDColor : threeDColor;
            }
        }
    }
}
