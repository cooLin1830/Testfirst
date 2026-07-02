using UnityEngine;

namespace DimensionShift
{
    public sealed class DimensionDoor : DimensionListenerBehaviour
    {
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private Renderer doorRenderer;
        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float moveSharpness = 8f;
        [SerializeField] private bool onlyBlocksIn2D;

        private bool open;
        private DimensionMode currentMode;

        private void Reset()
        {
            blockingCollider = GetComponent<Collider>();
            doorRenderer = GetComponentInChildren<Renderer>();
            closedLocalPosition = transform.localPosition;
        }

        private void Awake()
        {
            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider>();
            }

            if (doorRenderer == null)
            {
                doorRenderer = GetComponentInChildren<Renderer>();
            }

            closedLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            Vector3 target = open ? closedLocalPosition + openLocalOffset : closedLocalPosition;
            float t = 1f - Mathf.Exp(-moveSharpness * Time.deltaTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, t);

            if (blockingCollider != null)
            {
                blockingCollider.enabled = !open && (!onlyBlocksIn2D || currentMode == DimensionMode.TwoD);
            }

            if (doorRenderer != null)
            {
                doorRenderer.material.color = open ? new Color(0.45f, 0.9f, 0.58f) : new Color(0.95f, 0.35f, 0.28f);
            }
        }

        public void SetOpen(bool shouldOpen)
        {
            open = shouldOpen;
        }

        public void ToggleOpen()
        {
            open = !open;
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;
        }
    }
}
