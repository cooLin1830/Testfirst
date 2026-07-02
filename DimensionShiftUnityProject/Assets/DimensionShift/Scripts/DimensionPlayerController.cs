using UnityEngine;

namespace DimensionShift
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class DimensionPlayerController : DimensionListenerBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float twoDSpeed = 6f;
        [SerializeField] private float threeDSpeed = 5f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float airControl = 0.45f;
        [SerializeField] private float jumpVelocity = 7.5f;

        [Header("2D Slice")]
        [SerializeField] private float twoDPlaneZ = 0f;
        [SerializeField] private float snapToPlaneSpeed = 20f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private float groundCheckDistance = 0.24f;

        [Header("Input")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private float jumpBufferTime = 0.12f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private DimensionMode currentMode;
        private Vector3 spawnPosition;
        private float jumpBufferTimer;
        private bool isGrounded;
        private readonly Collider[] groundHits = new Collider[12];

        public DimensionMode CurrentMode => currentMode;
        public bool IsGrounded => isGrounded;

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
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            spawnPosition = transform.position;
        }

        private void Update()
        {
            if (Input.GetKeyDown(jumpKey))
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (Input.GetKeyDown(resetKey))
            {
                ResetToSpawn();
            }
        }

        private void FixedUpdate()
        {
            isGrounded = CheckGrounded();
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.fixedDeltaTime);
            Move();
            ClampTo2DPlaneIfNeeded();
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.constraints = RigidbodyConstraints.FreezeRotation;

            if (mode == DimensionMode.TwoD)
            {
                body.constraints |= RigidbodyConstraints.FreezePositionZ;
                Vector3 position = body.position;
                position.z = twoDPlaneZ;
                body.position = position;

                Vector3 velocity = body.velocity;
                velocity.z = 0f;
                body.velocity = velocity;
            }
        }

        public void ResetToSpawn()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.position = spawnPosition;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void Move()
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 desiredVelocity;
            if (currentMode == DimensionMode.TwoD)
            {
                desiredVelocity = new Vector3(input.x * twoDSpeed, body.velocity.y, 0f);
            }
            else
            {
                desiredVelocity = new Vector3(input.x * threeDSpeed, body.velocity.y, input.y * threeDSpeed);
            }

            float control = isGrounded ? 1f : airControl;
            Vector3 velocity = body.velocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 targetHorizontal = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, acceleration * control * Time.fixedDeltaTime);

            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (jumpBufferTimer > 0f && isGrounded)
            {
                velocity.y = jumpVelocity;
                jumpBufferTimer = 0f;
            }
            body.velocity = velocity;
        }

        private void ClampTo2DPlaneIfNeeded()
        {
            if (currentMode != DimensionMode.TwoD)
            {
                return;
            }

            Vector3 position = body.position;
            if (Mathf.Abs(position.z - twoDPlaneZ) > 0.001f)
            {
                position.z = Mathf.MoveTowards(position.z, twoDPlaneZ, snapToPlaneSpeed * Time.fixedDeltaTime);
                body.MovePosition(position);
            }
        }

        private bool CheckGrounded()
        {
            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }

            Bounds bounds = capsule.bounds;
            float probeRadius = Mathf.Min(
                groundCheckRadius,
                Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f));

            Vector3 probeCenter = new Vector3(
                bounds.center.x,
                bounds.min.y + probeRadius - 0.01f,
                bounds.center.z);

            int hitCount = Physics.OverlapSphereNonAlloc(
                probeCenter,
                probeRadius + groundCheckDistance,
                groundHits,
                groundMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundHits[i];
                if (hit == null || hit == capsule || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
