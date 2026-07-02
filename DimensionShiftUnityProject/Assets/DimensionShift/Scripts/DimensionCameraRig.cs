using UnityEngine;

namespace DimensionShift
{
    [RequireComponent(typeof(Camera))]
    public sealed class DimensionCameraRig : DimensionListenerBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 twoDOffset = new Vector3(0f, 4f, -16f);
        [SerializeField] private Vector3 threeDOffset = new Vector3(7f, 8f, -9f);
        [SerializeField] private Vector3 twoDRotation = new Vector3(8f, 0f, 0f);
        [SerializeField] private Vector3 threeDRotation = new Vector3(52f, -38f, 0f);
        [SerializeField] private float followSharpness = 9f;
        [SerializeField] private float rotationSharpness = 8f;
        [SerializeField] private float twoDOrthographicSize = 7.5f;
        [SerializeField] private float threeDFieldOfView = 55f;

        private Camera viewCamera;
        private DimensionMode currentMode;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 offset = currentMode == DimensionMode.TwoD ? twoDOffset : threeDOffset;
            Quaternion desiredRotation = Quaternion.Euler(currentMode == DimensionMode.TwoD ? twoDRotation : threeDRotation);
            Vector3 desiredPosition = target.position + offset;

            float followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);

            if (currentMode == DimensionMode.TwoD)
            {
                viewCamera.orthographic = true;
                viewCamera.orthographicSize = Mathf.Lerp(viewCamera.orthographicSize, twoDOrthographicSize, followT);
            }
            else
            {
                viewCamera.orthographic = false;
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, threeDFieldOfView, followT);
            }
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;

            if (viewCamera == null)
            {
                viewCamera = GetComponent<Camera>();
            }
        }
    }
}
