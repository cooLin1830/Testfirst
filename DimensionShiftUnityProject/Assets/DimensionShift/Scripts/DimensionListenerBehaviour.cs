using UnityEngine;

namespace DimensionShift
{
    public abstract class DimensionListenerBehaviour : MonoBehaviour, IDimensionModeListener
    {
        protected virtual void OnEnable()
        {
            Register();
        }

        protected virtual void Start()
        {
            Register();
        }

        protected virtual void OnDisable()
        {
            if (DimensionModeManager.Instance != null)
            {
                DimensionModeManager.Instance.Unregister(this);
            }
        }

        public abstract void SetDimensionMode(DimensionMode mode);

        private void Register()
        {
            if (DimensionModeManager.Instance != null)
            {
                DimensionModeManager.Instance.Register(this);
            }
        }
    }
}
