using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsPushBox : PetsPerspectiveListenerBehaviour
    {
        private GameObject twoDView;
        private GameObject topDownView;
        private Rigidbody topDownBody;
        private Collider twoDCollider;
        private Collider topDownCollider;
        private PetsLevelRuntime level;
        private PetsGridCoord coord;

        public PetsGridCoord Coord => coord;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord, GameObject twoDObject, GameObject topDownObject, Rigidbody rigidbody)
        {
            level = levelRuntime;
            coord = gridCoord;
            twoDView = twoDObject;
            topDownView = topDownObject;
            topDownBody = rigidbody;
            twoDCollider = twoDView != null ? twoDView.GetComponent<Collider>() : null;
            topDownCollider = topDownView != null ? topDownView.GetComponent<Collider>() : null;
            if (topDownBody != null)
            {
                topDownBody.isKinematic = true;
                topDownBody.useGravity = false;
                topDownBody.interpolation = RigidbodyInterpolation.None;
                topDownBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                topDownBody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }

            SetPerspectiveMode(PetsModeManager.Instance != null ? PetsModeManager.Instance.CurrentMode : PetsPerspectiveMode.TwoD);
        }

        public void MoveTo(PetsGridCoord targetCoord)
        {
            coord = targetCoord;
            if (level == null)
            {
                return;
            }

            if (twoDView != null)
            {
                twoDView.transform.position = level.GridToTwoDWorld(coord, -0.34f);
            }

            if (topDownView != null)
            {
                Vector3 target = level.GridToTopDownWorld(coord, 0.42f);
                topDownView.transform.position = target;
                if (topDownBody != null)
                {
                    topDownBody.position = target;
                    topDownBody.velocity = Vector3.zero;
                    topDownBody.angularVelocity = Vector3.zero;
                }
            }
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            bool inTwoD = mode == PetsPerspectiveMode.TwoD;
            if (twoDView != null)
            {
                twoDView.SetActive(inTwoD);
            }

            if (topDownView != null)
            {
                topDownView.SetActive(!inTwoD);
            }

            if (twoDCollider != null)
            {
                twoDCollider.enabled = inTwoD;
            }

            if (topDownCollider != null)
            {
                topDownCollider.enabled = !inTwoD;
            }

            if (topDownBody != null)
            {
                topDownBody.isKinematic = true;
                topDownBody.useGravity = false;
                topDownBody.velocity = Vector3.zero;
                topDownBody.angularVelocity = Vector3.zero;
                topDownBody.interpolation = RigidbodyInterpolation.None;
                topDownBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                topDownBody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
        }
    }
}
