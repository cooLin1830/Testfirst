using UnityEngine;
using UnityEngine.Events;

namespace DimensionShift
{
    public sealed class DimensionPickup : DimensionListenerBehaviour
    {
        [SerializeField] private bool pickupIn2D = true;
        [SerializeField] private bool pickupIn3D = true;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private Renderer pickupRenderer;
        [SerializeField] private Color availableColor = new Color(1f, 0.86f, 0.2f);
        [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f);

        public UnityEvent onPickedUp = new UnityEvent();

        private DimensionMode currentMode;
        private bool collected;

        private void Reset()
        {
            pickupRenderer = GetComponentInChildren<Renderer>();
        }

        private void Awake()
        {
            if (pickupRenderer == null)
            {
                pickupRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !CanPickupInCurrentMode())
            {
                return;
            }

            if (other.GetComponentInParent<DimensionPlayerController>() == null)
            {
                return;
            }

            collected = true;
            onPickedUp.Invoke();
            gameObject.SetActive(false);
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;
            if (pickupRenderer != null)
            {
                pickupRenderer.material.color = CanPickupInCurrentMode() ? availableColor : lockedColor;
            }
        }

        private bool CanPickupInCurrentMode()
        {
            return currentMode == DimensionMode.TwoD ? pickupIn2D : pickupIn3D;
        }
    }
}
