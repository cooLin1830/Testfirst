using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsPlayer2DAnimator : MonoBehaviour
    {
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsHorizontalJumpHash = Animator.StringToHash("IsHorizontalJump");

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float moveThreshold = 0.05f;
        [SerializeField] private float horizontalJumpThreshold = 0.1f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color invertedColor = Color.white;

        private int facingDirection = 1;

        private void Awake()
        {
            CacheReferences();
        }

        private void Reset()
        {
            CacheReferences();
        }

        public void ApplyState(PetsPerspectiveMode mode, Vector3 velocity, bool isGrounded)
        {
            CacheReferences();

            bool isTwoD = mode == PetsPerspectiveMode.TwoD;
            bool isMoving = isTwoD && Mathf.Abs(velocity.x) > moveThreshold;
            bool isHorizontalJump = isTwoD && Mathf.Abs(velocity.x) > horizontalJumpThreshold;
            bool animatorGrounded = !isTwoD || isGrounded;

            if (isTwoD && Mathf.Abs(velocity.x) > moveThreshold)
            {
                facingDirection = velocity.x >= 0f ? 1 : -1;
            }

            if (animator != null)
            {
                animator.SetBool(IsGroundedHash, animatorGrounded);
                animator.SetBool(IsMovingHash, isMoving);
                animator.SetBool(IsHorizontalJumpHash, isHorizontalJump);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facingDirection < 0;
            }
        }

        public void SetBlackRegionState(bool inside)
        {
            CacheReferences();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = inside ? invertedColor : normalColor;
            }
        }

        private void CacheReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
