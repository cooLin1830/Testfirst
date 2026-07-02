using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsSwitchTile : MonoBehaviour
    {
        [SerializeField] private PetsPerspectiveMode targetMode;
        [SerializeField] private bool isExit;

        private PetsLevelRuntime level;
        private PetsGridCoord coord;

        public PetsPerspectiveMode TargetMode => targetMode;
        public bool IsExit => isExit;
        public PetsGridCoord Coord => coord;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord, PetsPerspectiveMode target)
        {
            level = levelRuntime;
            coord = gridCoord;
            targetMode = target;
            isExit = false;
            EnsureTrigger();
        }

        public void ConfigureAsExit(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord)
        {
            level = levelRuntime;
            coord = gridCoord;
            isExit = true;
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            PetsLikePlayerController player = other.GetComponentInParent<PetsLikePlayerController>();
            if (player == null)
            {
                return;
            }

            if (isExit)
            {
                player.MarkReachedExit();
            }
        }

        private void EnsureTrigger()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.enabled = true;
            trigger.isTrigger = true;
        }
    }
}
