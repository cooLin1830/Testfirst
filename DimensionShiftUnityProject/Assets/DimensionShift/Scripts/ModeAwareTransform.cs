using UnityEngine;

namespace DimensionShift
{
    public sealed class ModeAwareTransform : DimensionListenerBehaviour
    {
        [SerializeField] private Vector3 twoDLocalPosition;
        [SerializeField] private Vector3 threeDLocalPosition;
        [SerializeField] private Vector3 twoDLocalEuler;
        [SerializeField] private Vector3 threeDLocalEuler;
        [SerializeField] private Vector3 twoDLocalScale = Vector3.one;
        [SerializeField] private Vector3 threeDLocalScale = Vector3.one;
        [SerializeField] private bool animate = true;
        [SerializeField] private float sharpness = 10f;

        private DimensionMode currentMode;

        private void Reset()
        {
            twoDLocalPosition = transform.localPosition;
            threeDLocalPosition = transform.localPosition;
            twoDLocalEuler = transform.localEulerAngles;
            threeDLocalEuler = transform.localEulerAngles;
            twoDLocalScale = transform.localScale;
            threeDLocalScale = transform.localScale;
        }

        private void Update()
        {
            if (!animate)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, TargetPosition(), t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(TargetEuler()), t);
            transform.localScale = Vector3.Lerp(transform.localScale, TargetScale(), t);
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;

            if (!animate)
            {
                transform.localPosition = TargetPosition();
                transform.localEulerAngles = TargetEuler();
                transform.localScale = TargetScale();
            }
        }

        public void Configure(
            Vector3 twoDPosition,
            Vector3 threeDPosition,
            Vector3 twoDEuler,
            Vector3 threeDEuler,
            Vector3 twoDScale,
            Vector3 threeDScale)
        {
            twoDLocalPosition = twoDPosition;
            threeDLocalPosition = threeDPosition;
            twoDLocalEuler = twoDEuler;
            threeDLocalEuler = threeDEuler;
            twoDLocalScale = twoDScale;
            threeDLocalScale = threeDScale;
        }

        private Vector3 TargetPosition()
        {
            return currentMode == DimensionMode.TwoD ? twoDLocalPosition : threeDLocalPosition;
        }

        private Vector3 TargetEuler()
        {
            return currentMode == DimensionMode.TwoD ? twoDLocalEuler : threeDLocalEuler;
        }

        private Vector3 TargetScale()
        {
            return currentMode == DimensionMode.TwoD ? twoDLocalScale : threeDLocalScale;
        }
    }
}
