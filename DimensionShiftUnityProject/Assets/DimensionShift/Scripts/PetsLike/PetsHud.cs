using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsHud : PetsPerspectiveListenerBehaviour
    {
        private const float DefaultPromptDuration = 2.2f;

        private static PetsHud activeHud;

        [SerializeField] private PetsLikePlayerController player;
        [SerializeField] private bool showHud;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.62f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color paperColor = new Color(0.98f, 0.97f, 0.91f, 0.97f);
        [SerializeField] private Color inkColor = new Color(0.08f, 0.07f, 0.06f, 1f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.22f);

        private PetsPerspectiveMode currentMode;
        private GUIStyle labelStyle;
        private GUIStyle promptStyle;
        private GUIStyle completionTitleStyle;
        private GUIStyle completionBodyStyle;
        private GUIStyle completionFooterStyle;
        private Texture2D panelTexture;
        private Texture2D paperTexture;
        private Texture2D inkTexture;
        private Texture2D shadowTexture;
        private Texture2D overlayTexture;
        private string promptText;
        private float promptUntil;
        private bool showCompletionScreen;

        public PetsLikePlayerController Player
        {
            get => player;
            set => player = value;
        }

        private void Awake()
        {
            activeHud = this;
            EnsureTextures();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            activeHud = this;
        }

        protected override void OnDisable()
        {
            if (activeHud == this)
            {
                activeHud = null;
            }

            base.OnDisable();
        }

        public static void ShowMissingStars(int collected, int total)
        {
            PetsHud hud = GetOrCreateActiveHud();
            int safeTotal = Mathf.Max(0, total);
            int safeCollected = safeTotal > 0 ? Mathf.Clamp(collected, 0, safeTotal) : Mathf.Max(0, collected);
            string text = safeTotal > 0
                ? $"还没有获取星星（{safeCollected}/{safeTotal}）"
                : "还没有获取星星";
            hud.ShowPrompt(text, DefaultPromptDuration);
        }

        public static void ShowCompletionScreen()
        {
            PetsHud hud = GetOrCreateActiveHud();
            hud.promptText = null;
            hud.showCompletionScreen = true;
        }

        private void OnGUI()
        {
            bool hasPrompt = !string.IsNullOrEmpty(promptText) && Time.unscaledTime < promptUntil;
            if (!showHud && !hasPrompt && !showCompletionScreen)
            {
                return;
            }

            EnsureStyles();

            if (showHud)
            {
                DrawOperationPanel();
            }

            if (hasPrompt)
            {
                DrawPrompt();
            }

            if (showCompletionScreen)
            {
                DrawCompletionScreen();
            }
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
        }

        private static PetsHud GetOrCreateActiveHud()
        {
            if (activeHud == null)
            {
                activeHud = Object.FindObjectOfType<PetsHud>();
            }

            if (activeHud == null)
            {
                activeHud = new GameObject("PETS HUD").AddComponent<PetsHud>();
            }

            return activeHud;
        }

        private void ShowPrompt(string text, float duration)
        {
            promptText = text;
            promptUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        }

        private void EnsureTextures()
        {
            panelTexture = CreateTexture(panelTexture, panelColor);
            paperTexture = CreateTexture(paperTexture, paperColor);
            inkTexture = CreateTexture(inkTexture, inkColor);
            shadowTexture = CreateTexture(shadowTexture, shadowColor);
            overlayTexture = CreateTexture(overlayTexture, new Color(0f, 0f, 0f, 0.2f));
        }

        private void EnsureStyles()
        {
            EnsureTextures();

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    richText = true,
                    wordWrap = true,
                    padding = new RectOffset(14, 14, 12, 12)
                };
                labelStyle.normal.textColor = textColor;
            }

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    padding = new RectOffset(14, 14, 8, 8)
                };
                promptStyle.normal.textColor = inkColor;
            }

            if (completionTitleStyle == null)
            {
                completionTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 34,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
                completionTitleStyle.normal.textColor = inkColor;
            }

            if (completionBodyStyle == null)
            {
                completionBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    wordWrap = true
                };
                completionBodyStyle.normal.textColor = inkColor;
            }

            if (completionFooterStyle == null)
            {
                completionFooterStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 17,
                    fontStyle = FontStyle.Italic,
                    wordWrap = true
                };
                completionFooterStyle.normal.textColor = inkColor;
            }
        }

        private static Texture2D CreateTexture(Texture2D existingTexture, Color color)
        {
            if (existingTexture == null)
            {
                existingTexture = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            existingTexture.SetPixel(0, 0, color);
            existingTexture.Apply();
            return existingTexture;
        }

        private void DrawOperationPanel()
        {
            GUI.DrawTexture(new Rect(18, 18, 520, 150), panelTexture);
            string starText = player != null && player.TotalStars > 0
                ? $"\nStars: <b>{player.CollectedStars}/{player.TotalStars}</b>"
                : string.Empty;
            string winText = player != null && player.ReachedExit ? "\n<b>Exit reached. Prototype complete.</b>" : string.Empty;
            string exitHint = player != null && !player.ReachedExit && player.TotalStars > 0 && player.CollectedStars < player.TotalStars
                ? "\nCollect every star before using EXIT."
                : string.Empty;
            GUI.Label(
                new Rect(18, 18, 520, 150),
                $"<b>PETS-like Mechanic Prototype</b>\nMode: <b>{ModeName()}</b>{starText}\nA/D: move in 2D    WASD: move in 2.5D\nSpace: jump    E: use 2D/2.5D switch tile    R: respawn\nBlack is safe in 2D but becomes a hole in 2.5D.{exitHint}{winText}",
                labelStyle);
        }

        private void DrawPrompt()
        {
            float width = Mathf.Min(500f, Mathf.Max(260f, Screen.width - 32f));
            Rect rect = new Rect((Screen.width - width) * 0.5f, Mathf.Max(24f, Screen.height * 0.12f), width, 76f);
            DrawPaperPanel(rect);
            GUI.Label(Inflate(rect, -14f), promptText, promptStyle);
        }

        private void DrawCompletionScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);

            float width = Mathf.Min(560f, Mathf.Max(300f, Screen.width - 40f));
            float height = Mathf.Min(280f, Mathf.Max(220f, Screen.height - 44f));
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawPaperPanel(rect);

            Rect inner = Inflate(rect, -28f);
            GUI.Label(new Rect(inner.x, inner.y + 8f, inner.width, 52f), "通关完成", completionTitleStyle);
            GUI.Label(new Rect(inner.x, inner.y + 82f, inner.width, 72f), "所有星星已收集，传送门已经稳定。", completionBodyStyle);
            GUI.Label(new Rect(inner.x, inner.y + inner.height - 44f, inner.width, 32f), "感谢游玩", completionFooterStyle);
        }

        private void DrawPaperPanel(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x + 5f, rect.y + 6f, rect.width, rect.height), shadowTexture);
            GUI.DrawTexture(rect, inkTexture);
            GUI.DrawTexture(Inflate(rect, -4f), paperTexture);
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private string ModeName()
        {
            return currentMode == PetsPerspectiveMode.TwoD ? "2D line platform" : "2.5D angled platform";
        }
    }
}
