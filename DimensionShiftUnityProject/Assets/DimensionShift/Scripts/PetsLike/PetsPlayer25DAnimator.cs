using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DimensionShift.PetsLike
{
    public sealed class PetsPlayer25DAnimator : MonoBehaviour
    {
        private enum Player25DAnimationState
        {
            Idle,
            Walk,
            Jump
        }

        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip jumpClip;
        [SerializeField] private float moveThreshold = 0.08f;
        [SerializeField] private float facingThreshold = 0.04f;
        [SerializeField] private float turnSpeed = 720f;

        private PlayableGraph graph;
        private AnimationClipPlayable activePlayable;
        private AnimationPlayableOutput output;
        private AnimationClip activeClip;
        private Player25DAnimationState activeState;
        private Quaternion targetFacingRotation;
        private Transform animatedRoot;
        private Vector3 lockedAnimatedRootPosition;
        private Quaternion lockedAnimatedRootRotation;
        private Vector3 lockedAnimatedRootScale;
        private bool hasActiveState;
        private bool hasFacingTarget;
        private bool hasLockedAnimatedRootPose;

        private void Awake()
        {
            CacheAnimator();
            CaptureAnimatedRootPose();
            CaptureFacing();
        }

        private void OnEnable()
        {
            CacheAnimator();
            CaptureAnimatedRootPose();
            CaptureFacing();
            PlayState(Player25DAnimationState.Idle, true);
        }

        private void Update()
        {
            LoopActiveClipIfNeeded();
        }

        private void LateUpdate()
        {
            RotateTowardFacing();
            StabilizeAnimatedRoot();
        }

        private void OnDisable()
        {
            DestroyGraph();
        }

        private void OnDestroy()
        {
            DestroyGraph();
        }

        public void Configure(Animator targetAnimator, AnimationClip idle, AnimationClip walk, AnimationClip jump)
        {
            animator = targetAnimator;
            idleClip = idle;
            walkClip = walk;
            jumpClip = jump;
            CacheAnimator();
            CaptureAnimatedRootPose();
            PlayState(Player25DAnimationState.Idle, true);
        }

        public void ApplyState(PetsPerspectiveMode mode, Vector3 velocity, bool isGrounded, bool isJumping)
        {
            if (mode != PetsPerspectiveMode.TwoPointFiveD)
            {
                return;
            }

            ApplyFacing(velocity);

            if (isJumping || !isGrounded)
            {
                PlayState(Player25DAnimationState.Jump, false);
                return;
            }

            Vector2 planarVelocity = new Vector2(velocity.x, velocity.z);
            PlayState(planarVelocity.magnitude > moveThreshold ? Player25DAnimationState.Walk : Player25DAnimationState.Idle, false);
        }

        private void ApplyFacing(Vector3 velocity)
        {
            Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (planarVelocity.sqrMagnitude <= facingThreshold * facingThreshold)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            targetFacingRotation = targetRotation;
            hasFacingTarget = true;
        }

        private void CaptureFacing()
        {
            if (hasFacingTarget)
            {
                return;
            }

            targetFacingRotation = transform.localRotation;
            hasFacingTarget = true;
        }

        private void RotateTowardFacing()
        {
            if (!hasFacingTarget)
            {
                return;
            }

            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetFacingRotation, turnSpeed * Time.deltaTime);
        }

        private void PlayState(Player25DAnimationState state, bool forceRestart)
        {
            AnimationClip clip = GetClipForState(state);
            if (clip == null || animator == null)
            {
                return;
            }

            if (!forceRestart && hasActiveState && activeState == state && activeClip == clip)
            {
                return;
            }

            EnsureGraph();

            if (activePlayable.IsValid())
            {
                activePlayable.Destroy();
            }

            activePlayable = AnimationClipPlayable.Create(graph, clip);
            activePlayable.SetApplyFootIK(false);
            activePlayable.SetTime(0f);
            activePlayable.SetSpeed(1f);
            output.SetSourcePlayable(activePlayable);

            activeState = state;
            activeClip = clip;
            hasActiveState = true;
        }

        private AnimationClip GetClipForState(Player25DAnimationState state)
        {
            if (state == Player25DAnimationState.Jump)
            {
                return jumpClip != null ? jumpClip : FirstAvailableClip();
            }

            if (state == Player25DAnimationState.Walk)
            {
                return walkClip != null ? walkClip : FirstAvailableClip();
            }

            return idleClip != null ? idleClip : FirstAvailableClip();
        }

        private AnimationClip FirstAvailableClip()
        {
            if (idleClip != null)
            {
                return idleClip;
            }

            if (walkClip != null)
            {
                return walkClip;
            }

            return jumpClip;
        }

        private void EnsureGraph()
        {
            if (graph.IsValid())
            {
                return;
            }

            graph = PlayableGraph.Create("PETS 2.5D Player Animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            graph.Play();
        }

        private void LoopActiveClipIfNeeded()
        {
            if (!activePlayable.IsValid() || activeClip == null)
            {
                return;
            }

            double length = activeClip.length;
            if (length <= 0.001d)
            {
                return;
            }

            double time = activePlayable.GetTime();
            bool shouldLoop = activeState != Player25DAnimationState.Jump;
            if (shouldLoop && time >= length)
            {
                activePlayable.SetTime(time % length);
            }
            else if (!shouldLoop && time > length)
            {
                activePlayable.SetTime(length);
            }
        }

        private void CacheAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }

        private void CaptureAnimatedRootPose()
        {
            if (animator == null)
            {
                hasLockedAnimatedRootPose = false;
                return;
            }

            animatedRoot = animator.transform;
            if (animatedRoot == null || animatedRoot == transform)
            {
                hasLockedAnimatedRootPose = false;
                return;
            }

            lockedAnimatedRootPosition = animatedRoot.localPosition;
            lockedAnimatedRootRotation = animatedRoot.localRotation;
            lockedAnimatedRootScale = animatedRoot.localScale;
            hasLockedAnimatedRootPose = true;
        }

        private void StabilizeAnimatedRoot()
        {
            if (!hasLockedAnimatedRootPose || animatedRoot == null)
            {
                return;
            }

            animatedRoot.localPosition = lockedAnimatedRootPosition;
            animatedRoot.localRotation = lockedAnimatedRootRotation;
            animatedRoot.localScale = lockedAnimatedRootScale;
        }

        private void DestroyGraph()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            activePlayable = default;
            output = default;
            activeClip = null;
            hasActiveState = false;
        }
    }
}
