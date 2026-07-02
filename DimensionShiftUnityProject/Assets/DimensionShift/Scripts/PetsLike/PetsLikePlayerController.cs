using System.Collections;
using UnityEngine;

namespace DimensionShift.PetsLike
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PetsLikePlayerController : PetsPerspectiveListenerBehaviour
    {
        [Header("References")]
        [SerializeField] private PetsLevelRuntime level;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private PetsPlayerVisualRig visualRig;
        [SerializeField] private PetsPlayer2DAnimator twoDAnimator;

        [Header("2D Movement")]
        [SerializeField] private float twoDMoveSpeed = 6f;
        [SerializeField] private float twoDAcceleration = 35f;
        [SerializeField] private float twoDJumpVelocity = 7f;
        [SerializeField] private float twoDJumpUpGravityScale = 1.25f;
        [SerializeField] private float twoDJumpDownGravityScale = 2.2f;
        [SerializeField] private float twoDWhiteStripClimbSpeed = 4.6f;
        [SerializeField] private float twoDGroundCheckRadius = 0.22f;
        [SerializeField] private float twoDGroundCheckDistance = 0.14f;

        [Header("Top Down Movement")]
        [SerializeField] private float topDownMoveSpeed = 4.8f;
        [SerializeField] private float topDownStepSnap = 10f;
        [SerializeField] private float jumpArcHeight = 0.9f;
        [SerializeField] private float jumpArcDuration = 0.28f;

        [Header("Input")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Visual")]
        [SerializeField] private Color normalColor = new Color(0.08f, 0.08f, 0.08f);
        [SerializeField] private Color invertedColor = Color.white;
        [SerializeField] private float twoDDefaultZ = 0f;
        [SerializeField] private float twoDBlackRegionFrontZ = -0.32f;

        private static PhysicMaterial noFrictionMaterial;

        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private readonly RaycastHit[] brickHits = new RaycastHit[8];
        private readonly System.Collections.Generic.List<PetsGridCoord> nearbyBlackRegions = new System.Collections.Generic.List<PetsGridCoord>(8);

        private Rigidbody body;
        private CapsuleCollider capsule;
        private PetsPerspectiveMode currentMode;
        private PetsGridCoord currentGridCoord;
        private Vector3 lastSafePosition;
        private bool inBlackRegion;
        private bool isGrounded2D;
        private bool hasTwoDGroundState;
        private bool isArcJumping;
        private bool reachedExit;
        private bool standingOnBlackTopEdge;
        private bool standingOnBlackBottomEdge;
        private bool isClimbingWhiteStrip2D;
        private bool hasBounceAirJump;
        private bool canTriggerBouncePad = true;
        private float jumpBufferTimer;
        private float previousTwoDBottom;
        private bool hasPreviousTwoDBounds;
        private Vector2Int lastTopDownDirection = Vector2Int.right;

        public PetsGridCoord CurrentGridCoord => currentGridCoord;
        public bool InBlackRegion => inBlackRegion;
        public bool ReachedExit => reachedExit;
        public int CollectedStars => level != null ? level.CollectedStars : 0;
        public int TotalStars => level != null ? level.TotalStars : 0;

        public void Configure(PetsLevelRuntime levelRuntime, PetsGridCoord spawn)
        {
            level = levelRuntime;
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            bodyRenderer = bodyRenderer != null ? bodyRenderer : GetComponentInChildren<Renderer>();
            visualRig = visualRig != null ? visualRig : GetComponentInChildren<PetsPlayerVisualRig>();
            twoDAnimator = twoDAnimator != null ? twoDAnimator : GetComponentInChildren<PetsPlayer2DAnimator>(true);
            SnapToGridCoord(spawn, PetsPerspectiveMode.TwoD);
            RecordSafePosition();
        }

        protected override void OnEnable()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            base.OnEnable();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (visualRig == null)
            {
                visualRig = GetComponentInChildren<PetsPlayerVisualRig>();
            }

            if (twoDAnimator == null)
            {
                twoDAnimator = GetComponentInChildren<PetsPlayer2DAnimator>(true);
            }

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            ConfigureCapsuleFriction();
        }

        private void Update()
        {
            if (Input.GetKeyDown(jumpKey))
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (Input.GetKeyDown(interactKey))
            {
                TrySwitchMode();
            }

            if (Input.GetKeyDown(resetKey))
            {
                RespawnAtLastSafePosition();
            }
        }

        private void FixedUpdate()
        {
            if (level == null || reachedExit)
            {
                return;
            }

            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.fixedDeltaTime);

            if (currentMode == PetsPerspectiveMode.TwoD)
            {
                TickTwoD();
            }
            else
            {
                currentGridCoord = ResolveRuleCoord();
                TickTopDown();
            }
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.constraints = RigidbodyConstraints.FreezeRotation;

            if (mode == PetsPerspectiveMode.TwoD)
            {
                body.useGravity = true;
                body.constraints |= RigidbodyConstraints.FreezePositionZ;
                hasTwoDGroundState = false;
                ApplyTwoDLayerDepth();
                CapturePreviousTwoDBounds();
            }
            else
            {
                hasTwoDGroundState = false;
                body.useGravity = false;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            UpdateTwoDAnimator();
        }

        public void SnapToGridCoord(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            if (level == null)
            {
                return;
            }

            currentGridCoord = coord;
            isGrounded2D = false;
            hasTwoDGroundState = false;
            standingOnBlackTopEdge = false;
            standingOnBlackBottomEdge = false;
            Vector3 target = mode == PetsPerspectiveMode.TwoD
                ? level.GridToTwoDWorld(coord, twoDDefaultZ) + Vector3.up * 0.55f
                : level.GridToTopDownWorld(coord, 0.55f);

            transform.position = target;
            if (body != null)
            {
                body.position = target;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void RecordSafePosition()
        {
            lastSafePosition = transform.position;
        }

        public void SetBlackRegionState(bool inside)
        {
            inBlackRegion = inside && currentMode == PetsPerspectiveMode.TwoD;
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = inBlackRegion ? invertedColor : normalColor;
            }

            if (visualRig != null)
            {
                visualRig.SetBlackRegionState(inBlackRegion);
            }

            if (twoDAnimator != null)
            {
                twoDAnimator.SetBlackRegionState(inBlackRegion);
            }

            ApplyTwoDLayerDepth();
        }

        public void MarkReachedExit()
        {
            if (level != null && level.CanReachExit(currentGridCoord))
            {
                reachedExit = true;
                body.velocity = Vector3.zero;
            }
        }

        private void TickTwoD()
        {
            if (!hasPreviousTwoDBounds)
            {
                CapturePreviousTwoDBounds();
            }

            standingOnBlackTopEdge = false;
            standingOnBlackBottomEdge = false;
            bool hadGroundState = hasTwoDGroundState;
            bool wasGroundedLastFrame = hadGroundState && isGrounded2D;
            isGrounded2D = CheckGrounded2D();
            float verticalVelocityAtGroundCheck = body.velocity.y;
            bool landedOn2DGround = hadGroundState
                && !wasGroundedLastFrame
                && isGrounded2D
                && verticalVelocityAtGroundCheck <= 0.05f;
            hasTwoDGroundState = true;
            currentGridCoord = ResolveTwoDRuleCoord();
            bool insideBlackRegion = level.IsBlackRegion(currentGridCoord) || IsOverlappingBlackRegion();
            bool wantsJump = jumpBufferTimer > 0f;
            if (TryResolveBlackTopSupport(out float supportY))
            {
                isGrounded2D = true;
                standingOnBlackTopEdge = true;
                if (!wantsJump)
                {
                    SnapToBlackTopSupport(supportY);
                }
            }
            else if (TryResolveBlackBottomSupport(out supportY))
            {
                isGrounded2D = true;
                standingOnBlackBottomEdge = true;
                if (!wantsJump)
                {
                    SnapToBlackBottomSupport(supportY);
                }
            }

            PetsGridCoord previousCoord = currentGridCoord;
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");
            Vector3 velocity = body.velocity;
            float targetX = inputX * twoDMoveSpeed;
            velocity.x = Mathf.MoveTowards(velocity.x, targetX, twoDAcceleration * Time.fixedDeltaTime);
            velocity.z = 0f;

            bool climbingWhiteStrip = TryApplyTwoDWhiteStripMovement(inputY, ref velocity);
            bool canUseGroundJump = isGrounded2D || standingOnBlackTopEdge || standingOnBlackBottomEdge;

            if (jumpBufferTimer > 0f && canUseGroundJump)
            {
                velocity.y = twoDJumpVelocity;
                jumpBufferTimer = 0f;
                standingOnBlackTopEdge = false;
                standingOnBlackBottomEdge = false;
                climbingWhiteStrip = false;
                hasBounceAirJump = false;
            }
            else if (jumpBufferTimer > 0f && hasBounceAirJump)
            {
                velocity.y = twoDJumpVelocity;
                jumpBufferTimer = 0f;
                climbingWhiteStrip = false;
                hasBounceAirJump = false;
            }

            if (isGrounded2D || standingOnBlackTopEdge || standingOnBlackBottomEdge)
            {
                if (body.velocity.y <= 0.05f)
                {
                    canTriggerBouncePad = true;
                    hasBounceAirJump = false;
                }
            }

            if (!climbingWhiteStrip)
            {
                ApplyTwoDJumpGravity(ref velocity);
            }

            SetTwoDGravity(!climbingWhiteStrip);
            body.velocity = velocity;
            ResolveBrickHeadHit();
            bool brokeLandingBrick = false;
            if (landedOn2DGround && level != null)
            {
                brokeLandingBrick = level.TryBreakFootLandingBrickNear(GetCapsuleBounds());
                if (brokeLandingBrick)
                {
                    isGrounded2D = false;
                }
            }

            PetsGridCoord nextCoord = ResolveTwoDRuleCoord();
            ApplyBlackRegionEdgeRules(ref nextCoord, previousCoord);

            if (level.IsValidPlayerCell(nextCoord, PetsPerspectiveMode.TwoD))
            {
                currentGridCoord = nextCoord;
                if ((isGrounded2D || standingOnBlackTopEdge || standingOnBlackBottomEdge) && !brokeLandingBrick)
                {
                    RecordSafePosition();
                }
            }

            SetBlackRegionState(insideBlackRegion && !standingOnBlackTopEdge);

            if (body.position.y < -3f)
            {
                RespawnAtLastSafePosition();
            }

            UpdateTwoDAnimator();
            CapturePreviousTwoDBounds();
        }

        private bool TryApplyTwoDWhiteStripMovement(float verticalInput, ref Vector3 velocity)
        {
            if (level == null)
            {
                return false;
            }

            int verticalDirection = verticalInput > 0.01f ? 1 : verticalInput < -0.01f ? -1 : 0;
            PetsGridCoord stripCoord = level.WorldToGrid(body.position, PetsPerspectiveMode.TwoD);
            if (!level.CanUseTwoDVerticalWhiteStrip(stripCoord, verticalDirection))
            {
                isClimbingWhiteStrip2D = false;
                return false;
            }

            Vector3 stripCenter = level.GridToTwoDWorld(stripCoord, twoDDefaultZ);
            Vector3 position = body.position;
            position.x = Mathf.MoveTowards(position.x, stripCenter.x, twoDAcceleration * Time.fixedDeltaTime);
            position.z = twoDDefaultZ;
            body.position = position;
            transform.position = position;

            velocity.y = verticalInput * twoDWhiteStripClimbSpeed;
            if (verticalDirection == 0)
            {
                velocity.y = 0f;
            }

            isGrounded2D = true;
            isClimbingWhiteStrip2D = true;
            return true;
        }

        public void BounceFromPad(float bounceVelocity)
        {
            if (currentMode != PetsPerspectiveMode.TwoD || body == null || !canTriggerBouncePad)
            {
                return;
            }

            Vector3 velocity = body.velocity;
            velocity.y = Mathf.Max(velocity.y, bounceVelocity);
            velocity.z = 0f;
            body.velocity = velocity;
            body.useGravity = true;
            isClimbingWhiteStrip2D = false;
            isGrounded2D = false;
            standingOnBlackTopEdge = false;
            standingOnBlackBottomEdge = false;
            hasBounceAirJump = true;
            canTriggerBouncePad = false;
            jumpBufferTimer = 0f;
        }

        private void SetTwoDGravity(bool enabled)
        {
            if (body == null || currentMode != PetsPerspectiveMode.TwoD || body.useGravity == enabled)
            {
                return;
            }

            body.useGravity = enabled;
            if (enabled || !isClimbingWhiteStrip2D)
            {
                return;
            }

            body.velocity = new Vector3(body.velocity.x, 0f, 0f);
        }

        private void TickTopDown()
        {
            if (isArcJumping)
            {
                return;
            }

            Vector2 rawInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2Int direction = ResolveCardinalDirection(rawInput);
            if (direction != Vector2Int.zero)
            {
                lastTopDownDirection = direction;
            }

            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = 0f;
                TryTopDownJump(lastTopDownDirection);
                return;
            }

            PetsGridCoord candidate = level.WorldToGrid(body.position, PetsPerspectiveMode.TwoPointFiveD);
            Vector3 move = new Vector3(rawInput.x, 0f, rawInput.y);
            move = Vector3.ClampMagnitude(move, 1f);
            Vector3 nextPosition = body.position + move * (topDownMoveSpeed * Time.fixedDeltaTime);
            PetsGridCoord nextCoord = level.WorldToGrid(nextPosition, PetsPerspectiveMode.TwoPointFiveD);
            Vector2Int moveDirection = ResolveCardinalDirection(rawInput);
            PetsGridCoord intendedCoord = currentGridCoord + new PetsGridCoord(moveDirection.x, moveDirection.y);
            bool enteringIntendedCell = moveDirection != Vector2Int.zero && nextCoord.Equals(intendedCoord);

            bool blockedByProp = level.IsTopDownBlockedByProp(nextCoord);
            if (blockedByProp && enteringIntendedCell && level.TryPushBox(nextCoord, moveDirection))
            {
                blockedByProp = false;
            }

            if (!blockedByProp && level.IsValidPlayerCell(nextCoord, PetsPerspectiveMode.TwoPointFiveD))
            {
                nextPosition.y = 0.55f;
                body.MovePosition(nextPosition);
                currentGridCoord = nextCoord;
                RecordSafePosition();
            }
            else if (level.IsTopDownHole(nextCoord))
            {
                RespawnAtLastSafePosition();
            }

            Vector3 snapTarget = level.GridToTopDownWorld(currentGridCoord, 0.55f);
            body.position = Vector3.Lerp(body.position, snapTarget, topDownStepSnap * Time.fixedDeltaTime * 0.25f);
            UpdateTwoDAnimator();
        }

        private void TryTopDownJump(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
            {
                return;
            }

            PetsGridCoord start = level.WorldToGrid(body.position, PetsPerspectiveMode.TwoPointFiveD);
            PetsGridCoord step = new PetsGridCoord(direction.x, direction.y);
            PetsGridCoord adjacent = start + step;
            PetsGridCoord target = adjacent;

            if (level.IsTopDownBlockedByProp(adjacent))
            {
                return;
            }

            if (!level.IsValidPlayerCell(adjacent, PetsPerspectiveMode.TwoPointFiveD))
            {
                if (level.IsTopDownJumpableHole(adjacent))
                {
                    PetsGridCoord landing = adjacent + step;
                    if (!level.IsTopDownBlockedByProp(landing) && level.IsValidPlayerCell(landing, PetsPerspectiveMode.TwoPointFiveD))
                    {
                        target = landing;
                    }
                    else
                    {
                        RespawnAtLastSafePosition();
                        return;
                    }
                }
                else
                {
                    RespawnAtLastSafePosition();
                    return;
                }
            }

            StartCoroutine(TopDownJumpArc(start, target));
        }

        private void ResolveBrickHeadHit()
        {
            if (body == null || body.velocity.y <= 0.05f)
            {
                return;
            }

            Bounds bounds = GetCapsuleBounds();
            Vector3 headProbe = new Vector3(bounds.center.x, bounds.max.y - 0.02f, bounds.center.z);
            Vector3 halfExtents = new Vector3(bounds.extents.x * 0.72f, 0.05f, 0.32f);
            float castDistance = Mathf.Max(0.08f, body.velocity.y * Time.fixedDeltaTime + 0.1f);
            int hitCount = Physics.BoxCastNonAlloc(
                headProbe,
                halfExtents,
                Vector3.up,
                brickHits,
                Quaternion.identity,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = brickHits[i].collider;
                if (hit == null || hit == capsule || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                PetsBreakableBrick brick = hit.GetComponentInParent<PetsBreakableBrick>();
                if (brick == null || brick.IsBroken || !brick.CanBreakFromTwoDHeadHit)
                {
                    continue;
                }

                brick.Break();
                Vector3 velocity = body.velocity;
                velocity.y = Mathf.Min(velocity.y, 0f);
                body.velocity = velocity;
                return;
            }
        }

        private IEnumerator TopDownJumpArc(PetsGridCoord start, PetsGridCoord target)
        {
            isArcJumping = true;
            body.velocity = Vector3.zero;

            Vector3 from = level.GridToTopDownWorld(start, 0.55f);
            Vector3 to = level.GridToTopDownWorld(target, 0.55f);
            float elapsed = 0f;

            while (elapsed < jumpArcDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpArcDuration);
                float arc = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
                Vector3 position = Vector3.Lerp(from, to, t);
                position.y += arc;
                body.MovePosition(position);
                yield return null;
            }

            body.position = to;
            currentGridCoord = target;
            isArcJumping = false;
            RecordSafePosition();
            UpdateTwoDAnimator();
        }

        private void TrySwitchMode()
        {
            if (level == null || PetsModeManager.Instance == null)
            {
                return;
            }

            PetsPerspectiveMode targetMode = currentMode == PetsPerspectiveMode.TwoD
                ? PetsPerspectiveMode.TwoPointFiveD
                : PetsPerspectiveMode.TwoD;

            currentGridCoord = ResolveRuleCoord();
            PetsModeManager.Instance.TrySetMode(targetMode, this, level);
        }

        private void RespawnAtLastSafePosition()
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = lastSafePosition;
            transform.position = lastSafePosition;
            hasBounceAirJump = false;
            canTriggerBouncePad = true;
            isGrounded2D = false;
            hasTwoDGroundState = false;
            standingOnBlackTopEdge = false;
            standingOnBlackBottomEdge = false;
            currentGridCoord = level != null ? ResolveRuleCoord() : currentGridCoord;
            SetBlackRegionState(level != null && level.IsBlackRegion(currentGridCoord));
            UpdateTwoDAnimator();
            CapturePreviousTwoDBounds();
        }

        private void UpdateTwoDAnimator()
        {
            if (twoDAnimator == null)
            {
                twoDAnimator = GetComponentInChildren<PetsPlayer2DAnimator>(true);
            }

            if (twoDAnimator == null || body == null)
            {
                return;
            }

            bool grounded = isGrounded2D || standingOnBlackTopEdge || standingOnBlackBottomEdge;
            twoDAnimator.ApplyState(currentMode, body.velocity, grounded);
        }

        private void ApplyBlackRegionEdgeRules(ref PetsGridCoord nextCoord, PetsGridCoord fallbackCoord)
        {
            Bounds bounds = GetCapsuleBounds();
            float currentBottom = bounds.min.y;
            bool fallingMeaningfully = body.velocity.y < -0.05f || previousTwoDBottom - currentBottom > 0.015f;

            level.CollectBlackRegionsNear(bounds, 0.12f, nearbyBlackRegions);
            for (int i = 0; i < nearbyBlackRegions.Count; i++)
            {
                PetsGridCoord nearbyBlack = nearbyBlackRegions[i];
                Vector3 blackCenter = level.GridToTwoDWorld(nearbyBlack, twoDDefaultZ);
                float leftEdge = blackCenter.x - level.CellSize * 0.5f;
                float rightEdge = blackCenter.x + level.CellSize * 0.5f;
                float topEdge = blackCenter.y + level.CellSize * 0.5f;
                float bottomEdge = blackCenter.y - level.CellSize * 0.5f;

                bool overlapsBlackHorizontally = bounds.max.x > leftEdge && bounds.min.x < rightEdge;
                bool isOuterTopEdge = !level.IsBlackRegion(nearbyBlack + new PetsGridCoord(0, 1));
                bool isOuterBottomEdge = !level.IsBlackRegion(nearbyBlack + new PetsGridCoord(0, -1));
                bool crossedTopEdgeDownward = previousTwoDBottom >= topEdge && currentBottom < topEdge;
                bool crossedBottomEdgeDownward = previousTwoDBottom >= bottomEdge && currentBottom < bottomEdge;

                if (fallingMeaningfully && overlapsBlackHorizontally && isOuterTopEdge && crossedTopEdgeDownward)
                {
                    BlockTwoDTopEntry(topEdge);
                    standingOnBlackTopEdge = true;
                    nextCoord = ResolveTwoDRuleCoord();
                    return;
                }

                if (overlapsBlackHorizontally && isOuterBottomEdge && crossedBottomEdgeDownward)
                {
                    BlockTwoDDownExit(bottomEdge);
                    standingOnBlackBottomEdge = true;
                    nextCoord = ResolveTwoDRuleCoord();
                    return;
                }
            }
        }

        private void BlockTwoDTopEntry(float topEdge)
        {
            float halfHeight = GetCapsuleHalfHeight();
            Vector3 blocked = body.position;
            blocked.y = topEdge + halfHeight + 0.02f;
            body.position = blocked;
            transform.position = blocked;
            body.velocity = new Vector3(body.velocity.x, Mathf.Max(0f, body.velocity.y), 0f);
            CapturePreviousTwoDBounds();
        }

        private bool TryResolveBlackTopSupport(out float supportY)
        {
            supportY = 0f;
            if (level == null || body == null || body.velocity.y > 0.05f)
            {
                return false;
            }

            Bounds bounds = GetCapsuleBounds();
            float footY = bounds.min.y;
            level.CollectBlackRegionsNear(bounds, 0.18f, nearbyBlackRegions);
            for (int i = 0; i < nearbyBlackRegions.Count; i++)
            {
                PetsGridCoord black = nearbyBlackRegions[i];
                if (level.IsBlackRegion(black + new PetsGridCoord(0, 1)))
                {
                    continue;
                }

                Vector3 blackCenter = level.GridToTwoDWorld(black, twoDDefaultZ);
                float leftEdge = blackCenter.x - level.CellSize * 0.5f;
                float rightEdge = blackCenter.x + level.CellSize * 0.5f;
                float topEdge = blackCenter.y + level.CellSize * 0.5f;
                bool overlapsX = bounds.max.x > leftEdge + 0.04f && bounds.min.x < rightEdge - 0.04f;
                bool footNearTop = footY >= topEdge - 0.16f && footY <= topEdge + twoDGroundCheckDistance + 0.16f;
                if (overlapsX && footNearTop)
                {
                    supportY = topEdge;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveBlackBottomSupport(out float supportY)
        {
            supportY = 0f;
            if (level == null || body == null || body.velocity.y > 0.05f)
            {
                return false;
            }

            Bounds bounds = GetCapsuleBounds();
            float footY = bounds.min.y;
            level.CollectBlackRegionsNear(bounds, 0.18f, nearbyBlackRegions);
            for (int i = 0; i < nearbyBlackRegions.Count; i++)
            {
                PetsGridCoord black = nearbyBlackRegions[i];
                if (level.IsBlackRegion(black + new PetsGridCoord(0, -1)))
                {
                    continue;
                }

                Vector3 blackCenter = level.GridToTwoDWorld(black, twoDDefaultZ);
                float leftEdge = blackCenter.x - level.CellSize * 0.5f;
                float rightEdge = blackCenter.x + level.CellSize * 0.5f;
                float topEdge = blackCenter.y + level.CellSize * 0.5f;
                float bottomEdge = blackCenter.y - level.CellSize * 0.5f;
                bool overlapsX = bounds.max.x > leftEdge + 0.04f && bounds.min.x < rightEdge - 0.04f;
                bool overlapsBlackVertically = bounds.max.y > bottomEdge + 0.04f && bounds.min.y < topEdge - 0.04f;
                bool footNearBottom = footY >= bottomEdge - 0.08f && footY <= bottomEdge + twoDGroundCheckDistance + 0.18f;
                if (overlapsX && overlapsBlackVertically && footNearBottom)
                {
                    supportY = bottomEdge;
                    return true;
                }
            }

            return false;
        }

        private bool IsOverlappingBlackRegion()
        {
            if (level == null)
            {
                return false;
            }

            Bounds bounds = GetCapsuleBounds();
            level.CollectBlackRegionsNear(bounds, 0.02f, nearbyBlackRegions);
            float halfSize = level.CellSize * 0.5f;
            for (int i = 0; i < nearbyBlackRegions.Count; i++)
            {
                Vector3 center = level.GridToTwoDWorld(nearbyBlackRegions[i], twoDDefaultZ);
                bool overlapsX = bounds.max.x > center.x - halfSize + 0.02f
                    && bounds.min.x < center.x + halfSize - 0.02f;
                bool overlapsY = bounds.max.y > center.y - halfSize + 0.02f
                    && bounds.min.y < center.y + halfSize - 0.02f;
                if (overlapsX && overlapsY)
                {
                    return true;
                }
            }

            return false;
        }

        private void SnapToBlackTopSupport(float supportY)
        {
            float halfHeight = GetCapsuleHalfHeight();
            Vector3 supported = body.position;
            supported.y = supportY + halfHeight + 0.01f;
            supported.z = twoDDefaultZ;
            body.position = supported;
            transform.position = supported;
            body.velocity = new Vector3(body.velocity.x, Mathf.Max(0f, body.velocity.y), 0f);
        }

        private void SnapToBlackBottomSupport(float supportY)
        {
            float halfHeight = GetCapsuleHalfHeight();
            Vector3 supported = body.position;
            supported.y = supportY + halfHeight + 0.02f;
            supported.z = twoDDefaultZ;
            body.position = supported;
            transform.position = supported;
            body.velocity = new Vector3(body.velocity.x, Mathf.Max(0f, body.velocity.y), 0f);
        }

        private void BlockTwoDDownExit(float bottomEdge)
        {
            float halfHeight = GetCapsuleHalfHeight();
            Vector3 blocked = body.position;
            blocked.y = bottomEdge + halfHeight + 0.02f;
            body.position = blocked;
            transform.position = blocked;
            body.velocity = new Vector3(body.velocity.x, Mathf.Max(0f, body.velocity.y), 0f);
            CapturePreviousTwoDBounds();
        }

        private PetsGridCoord ResolveRuleCoord()
        {
            if (level == null)
            {
                return currentGridCoord;
            }

            return currentMode == PetsPerspectiveMode.TwoD
                ? ResolveTwoDRuleCoord()
                : level.WorldToGrid(body.position, PetsPerspectiveMode.TwoPointFiveD);
        }

        private PetsGridCoord ResolveTwoDRuleCoord()
        {
            PetsGridCoord bodyCoord = level.WorldToGrid(body.position, PetsPerspectiveMode.TwoD);
            if (level.IsBlackRegion(bodyCoord))
            {
                return bodyCoord;
            }

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }

            Bounds bounds = capsule.bounds;
            Vector3 footProbe = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);
            if (TryResolveBlackTopSupportCoord(footProbe, out PetsGridCoord supportCoord))
            {
                return supportCoord;
            }

            return level.WorldToGrid(footProbe, PetsPerspectiveMode.TwoD);
        }

        private bool TryResolveBlackTopSupportCoord(Vector3 footProbe, out PetsGridCoord supportCoord)
        {
            supportCoord = default;
            if (!level.TryFindBlackRegionNear(footProbe, 0.08f, out PetsGridCoord blackCoord))
            {
                return false;
            }

            Vector3 blackCenter = level.GridToTwoDWorld(blackCoord, twoDDefaultZ);
            float topEdge = blackCenter.y + level.CellSize * 0.5f;
            bool isOuterTopEdge = !level.IsBlackRegion(blackCoord + new PetsGridCoord(0, 1));
            bool footIsOnTopEdge = Mathf.Abs(footProbe.y - topEdge) <= 0.08f && footProbe.y >= topEdge - 0.02f;
            if (!isOuterTopEdge || !footIsOnTopEdge)
            {
                return false;
            }

            supportCoord = blackCoord + new PetsGridCoord(0, 1);
            return level.IsValidPlayerCell(supportCoord, PetsPerspectiveMode.TwoD);
        }

        private void ApplyTwoDLayerDepth()
        {
            if (body == null || currentMode != PetsPerspectiveMode.TwoD)
            {
                return;
            }

            float targetZ = inBlackRegion ? twoDBlackRegionFrontZ : twoDDefaultZ;
            Vector3 position = body.position;
            if (Mathf.Abs(position.z - targetZ) < 0.001f)
            {
                return;
            }

            position.z = targetZ;
            body.position = position;
            transform.position = position;
        }

        private float GetCapsuleHalfHeight()
        {
            return GetCapsuleBounds().extents.y;
        }

        private Bounds GetCapsuleBounds()
        {
            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }

            return capsule != null ? capsule.bounds : new Bounds(body != null ? body.position : transform.position, new Vector3(0.45f, 0.9f, 0.45f));
        }

        private void CapturePreviousTwoDBounds()
        {
            if (currentMode != PetsPerspectiveMode.TwoD)
            {
                hasPreviousTwoDBounds = false;
                return;
            }

            Bounds bounds = GetCapsuleBounds();
            previousTwoDBottom = bounds.min.y;
            hasPreviousTwoDBounds = true;
        }

        private bool CheckGrounded2D()
        {
            Bounds bounds = capsule.bounds;
            float probeRadius = Mathf.Min(twoDGroundCheckRadius, Mathf.Max(0.04f, bounds.extents.x * 0.62f));
            Vector3 probeCenter = new Vector3(bounds.center.x, bounds.min.y + probeRadius + 0.03f, bounds.center.z);
            int hitCount = Physics.SphereCastNonAlloc(
                probeCenter,
                probeRadius,
                Vector3.down,
                groundHits,
                twoDGroundCheckDistance + 0.06f,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundHits[i].collider;
                if (hit == null || hit == capsule || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (groundHits[i].normal.y >= 0.62f)
                {
                    return true;
                }
            }

            return false;
        }

        private void ConfigureCapsuleFriction()
        {
            if (capsule == null)
            {
                return;
            }

            if (noFrictionMaterial == null)
            {
                noFrictionMaterial = new PhysicMaterial("PETS Player No Friction")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicMaterialCombine.Minimum,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            capsule.material = noFrictionMaterial;
        }

        private void ApplyTwoDJumpGravity(ref Vector3 velocity)
        {
            if (isGrounded2D || standingOnBlackTopEdge || standingOnBlackBottomEdge)
            {
                return;
            }

            float gravityScale = velocity.y > 0f ? twoDJumpUpGravityScale : twoDJumpDownGravityScale;
            velocity += Physics.gravity * ((gravityScale - 1f) * Time.fixedDeltaTime);
        }

        private static Vector2Int ResolveCardinalDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
            {
                return input.x >= 0f ? Vector2Int.right : Vector2Int.left;
            }

            return input.y >= 0f ? Vector2Int.up : Vector2Int.down;
        }
    }
}
