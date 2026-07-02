using UnityEngine;

namespace DimensionShift.PetsLike
{
    [RequireComponent(typeof(Collider))]
    public sealed class PetsBlackRegion2D : MonoBehaviour
    {
        private PetsLevelRuntime level;
        private PetsGridCoord coord;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord)
        {
            level = levelRuntime;
            coord = gridCoord;
            Collider trigger = GetComponent<Collider>();
            trigger.enabled = true;
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PetsLikePlayerController player = other.GetComponentInParent<PetsLikePlayerController>();
            if (player != null)
            {
                player.SetBlackRegionState(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PetsLikePlayerController player = other.GetComponentInParent<PetsLikePlayerController>();
            if (player != null && level != null && !level.IsBlackRegion(player.CurrentGridCoord))
            {
                player.SetBlackRegionState(false);
            }
        }
    }
}
