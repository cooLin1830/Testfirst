using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsSpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private bool resetOnEnable = true;

        private float elapsedFrames;
        private int currentFrame = -1;

        public void Configure(SpriteRenderer targetRenderer, Sprite[] animationFrames, float frameRate)
        {
            spriteRenderer = targetRenderer;
            frames = animationFrames;
            framesPerSecond = Mathf.Max(0.01f, frameRate);
            ResetAnimation();
        }

        private void Awake()
        {
            CacheRenderer();
        }

        private void OnEnable()
        {
            if (resetOnEnable)
            {
                ResetAnimation();
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            CacheRenderer();
            elapsedFrames += Time.deltaTime * framesPerSecond;
            ApplyFrame(Mathf.FloorToInt(elapsedFrames) % frames.Length);
        }

        private void ResetAnimation()
        {
            elapsedFrames = 0f;
            ApplyFrame(0);
        }

        private void ApplyFrame(int frame)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            CacheRenderer();
            if (spriteRenderer == null || frame == currentFrame)
            {
                return;
            }

            currentFrame = frame;
            spriteRenderer.sprite = frames[frame];
        }

        private void CacheRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }
}
