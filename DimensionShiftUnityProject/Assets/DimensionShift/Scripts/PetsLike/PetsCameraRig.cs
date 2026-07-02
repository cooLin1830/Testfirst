using UnityEngine;

namespace DimensionShift.PetsLike
{
    [RequireComponent(typeof(Camera))]
    public sealed class PetsCameraRig : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 twoDOffset = new Vector3(0f, 3.8f, -13f);
        [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 7.2f, -9.2f);
        [SerializeField] private Vector3 twoDRotation = new Vector3(6f, 0f, 0f);
        [SerializeField] private Vector3 topDownRotation = new Vector3(46f, 0f, 0f);
        [SerializeField] private float twoDSize = 6.5f;
        [SerializeField] private float topDownFieldOfView = 46f;
        [SerializeField] private float topDownFocusAhead = 1.8f;
        [SerializeField] private float followSharpness = 10f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float lensSharpness = 12f;

        private Camera viewCamera;
        private PetsPerspectiveMode currentMode;

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

            Vector3 offset = currentMode == PetsPerspectiveMode.TwoD ? twoDOffset : topDownOffset;
            Vector3 focus = currentMode == PetsPerspectiveMode.TwoD
                ? new Vector3(target.position.x, target.position.y, 0f)
                : new Vector3(target.position.x, 0f, target.position.z + topDownFocusAhead);

            Quaternion desiredRotation = Quaternion.Euler(currentMode == PetsPerspectiveMode.TwoD ? twoDRotation : topDownRotation);
            Vector3 desiredPosition = focus + offset;

            float followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
            ApplyLens(currentMode, followT);
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
            if (viewCamera == null)
            {
                viewCamera = GetComponent<Camera>();
            }
        }

        private void ApplyLens(PetsPerspectiveMode mode, float followT)
        {
            if (mode == PetsPerspectiveMode.TwoD)
            {
                if (!viewCamera.orthographic)
                {
                    viewCamera.orthographic = true;
                    viewCamera.orthographicSize = twoDSize;
                }

                viewCamera.orthographicSize = Mathf.Lerp(viewCamera.orthographicSize, twoDSize, followT);
                return;
            }

            if (viewCamera.orthographic)
            {
                viewCamera.orthographic = false;
                viewCamera.fieldOfView = topDownFieldOfView;
            }

            float lensT = 1f - Mathf.Exp(-lensSharpness * Time.deltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, topDownFieldOfView, lensT);
        }
    }
}
