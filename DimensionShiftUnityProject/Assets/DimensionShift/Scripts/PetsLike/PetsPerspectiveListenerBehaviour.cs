using UnityEngine;

namespace DimensionShift.PetsLike
{
    public abstract class PetsPerspectiveListenerBehaviour : MonoBehaviour, IPetsPerspectiveListener
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
            if (PetsModeManager.Instance != null)
            {
                PetsModeManager.Instance.Unregister(this);
            }
        }

        public abstract void SetPerspectiveMode(PetsPerspectiveMode mode);

        private void Register()
        {
            if (PetsModeManager.Instance != null)
            {
                PetsModeManager.Instance.Register(this);
            }
        }
    }
}
