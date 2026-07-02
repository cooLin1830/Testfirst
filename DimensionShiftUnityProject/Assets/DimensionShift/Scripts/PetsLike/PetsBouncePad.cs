using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsBouncePad : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private float bounceVelocity = 10.5f;

        private GameObject twoDView;
        private Collider twoDTrigger;

        public void Configure(GameObject twoDObject, Collider trigger, float velocity)
        {
            twoDView = twoDObject;
            twoDTrigger = trigger;
            bounceVelocity = velocity;
            SetPerspectiveMode(PetsModeManager.Instance != null ? PetsModeManager.Instance.CurrentMode : PetsPerspectiveMode.TwoD);
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            bool active = mode == PetsPerspectiveMode.TwoD;
            if (twoDView != null)
            {
                twoDView.SetActive(active);
            }

            if (twoDTrigger != null)
            {
                twoDTrigger.enabled = active;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerBounce(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerBounce(other);
        }

        private void TriggerBounce(Collider other)
        {
            if (PetsModeManager.Instance == null || !PetsModeManager.Instance.Is2D)
            {
                return;
            }

            PetsLikePlayerController player = other.GetComponentInParent<PetsLikePlayerController>();
            if (player == null)
            {
                return;
            }

            player.BounceFromPad(bounceVelocity);
        }
    }
}
