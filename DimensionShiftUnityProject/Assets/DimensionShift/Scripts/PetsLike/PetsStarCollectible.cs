using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsStarCollectible : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private GameObject twoDView;
        [SerializeField] private GameObject topDownView;
        [SerializeField] private BoxCollider trigger;
        [SerializeField] private float coveredTwoDDepth = -0.28f;
        [SerializeField] private float visibleTwoDDepth = -0.46f;

        private PetsLevelRuntime level;
        private PetsGridCoord coord;
        private bool collected;
        private bool coveredInTwoD;
        private PetsPerspectiveMode currentMode;
        private float cellSize = 1f;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord, GameObject twoDVisual, GameObject topDownVisual, float coveredDepth, float visibleDepth)
        {
            level = levelRuntime;
            coord = gridCoord;
            twoDView = twoDVisual;
            topDownView = topDownVisual;
            coveredTwoDDepth = coveredDepth;
            visibleTwoDDepth = visibleDepth;
            trigger = trigger != null ? trigger : GetComponent<BoxCollider>();
            cellSize = levelRuntime != null ? levelRuntime.CellSize : 1f;
            ApplyMode();
        }

        public void SetCoveredInTwoD(bool covered)
        {
            coveredInTwoD = covered;
            ApplyTwoDVisualDepth();
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
            ApplyMode();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || other.GetComponentInParent<PetsLikePlayerController>() == null)
            {
                return;
            }

            collected = true;
            level?.NotifyStarCollected(coord);
            ApplyMode();
        }

        private void ApplyMode()
        {
            if (twoDView != null)
            {
                twoDView.SetActive(!collected && currentMode == PetsPerspectiveMode.TwoD);
                ApplyTwoDVisualDepth();
            }

            if (topDownView != null)
            {
                topDownView.SetActive(!collected && currentMode == PetsPerspectiveMode.TwoPointFiveD);
            }

            if (trigger == null)
            {
                trigger = GetComponent<BoxCollider>();
            }

            if (trigger == null)
            {
                return;
            }

            trigger.enabled = !collected;
            if (currentMode == PetsPerspectiveMode.TwoD)
            {
                transform.position = level != null
                    ? level.GridToTwoDWorld(coord, 0f)
                    : new Vector3(coord.x, coord.y, 0f);
                transform.rotation = Quaternion.identity;
                trigger.size = new Vector3(cellSize * 0.76f, cellSize * 0.76f, 0.8f);
                return;
            }

            transform.position = level != null
                ? level.GridToTopDownWorld(coord, 0.48f)
                : new Vector3(coord.x, 0.48f, coord.y);
            transform.rotation = Quaternion.identity;
            trigger.size = new Vector3(cellSize * 0.76f, cellSize * 0.78f, cellSize * 0.76f);
        }

        private void ApplyTwoDVisualDepth()
        {
            if (twoDView == null)
            {
                return;
            }

            Vector3 position = twoDView.transform.position;
            position.z = coveredInTwoD ? coveredTwoDDepth : visibleTwoDDepth;
            twoDView.transform.position = position;
        }
    }
}
