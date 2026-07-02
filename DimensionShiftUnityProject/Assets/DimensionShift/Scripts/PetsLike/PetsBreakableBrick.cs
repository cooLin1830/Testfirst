using UnityEngine;

namespace DimensionShift.PetsLike
{
    public enum PetsBreakablePropRule
    {
        FootLanding,
        TwoDHeadHit
    }

    public sealed class PetsBreakableBrick : PetsPerspectiveListenerBehaviour
    {
        private GameObject twoDView;
        private GameObject topDownView;
        private Collider twoDCollider;
        private Collider topDownCollider;
        private PetsLevelRuntime level;
        private PetsGridCoord coord;
        private PetsBreakablePropRule breakRule;
        private bool broken;
        private bool hasFootLanding;

        public PetsGridCoord Coord => coord;
        public bool IsBroken => broken;
        public bool CanBreakFromFootLanding => breakRule == PetsBreakablePropRule.FootLanding;
        public bool CanBreakFromTwoDHeadHit => breakRule == PetsBreakablePropRule.FootLanding
            || breakRule == PetsBreakablePropRule.TwoDHeadHit;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord gridCoord, GameObject twoDObject, GameObject topDownObject, PetsBreakablePropRule rule)
        {
            level = levelRuntime;
            coord = gridCoord;
            twoDView = twoDObject;
            topDownView = topDownObject;
            twoDCollider = twoDView != null ? twoDView.GetComponent<Collider>() : null;
            topDownCollider = topDownView != null ? topDownView.GetComponent<Collider>() : null;
            breakRule = rule;
            SetPerspectiveMode(PetsModeManager.Instance != null ? PetsModeManager.Instance.CurrentMode : PetsPerspectiveMode.TwoD);
        }

        public void Break()
        {
            if (broken)
            {
                return;
            }

            broken = true;
            if (level != null)
            {
                level.NotifyBrickBroken(coord);
            }

            SetActive(false, false, false);
        }

        public bool RegisterFootLanding()
        {
            if (broken || !CanBreakFromFootLanding)
            {
                return false;
            }

            if (!hasFootLanding)
            {
                hasFootLanding = true;
                return false;
            }

            Break();
            return true;
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            if (broken)
            {
                SetActive(false, false, false);
                return;
            }

            bool inTwoD = mode == PetsPerspectiveMode.TwoD;
            SetActive(inTwoD, !inTwoD, true);
        }

        private void SetActive(bool showTwoD, bool showTopDown, bool collidersEnabled)
        {
            if (twoDView != null)
            {
                twoDView.SetActive(showTwoD);
            }

            if (topDownView != null)
            {
                topDownView.SetActive(showTopDown);
            }

            if (twoDCollider != null)
            {
                twoDCollider.enabled = showTwoD && collidersEnabled;
            }

            if (topDownCollider != null)
            {
                topDownCollider.enabled = showTopDown && collidersEnabled;
            }
        }
    }
}
