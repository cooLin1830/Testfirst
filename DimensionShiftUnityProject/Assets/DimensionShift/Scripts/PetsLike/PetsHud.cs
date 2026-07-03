using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsHud : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private PetsLikePlayerController player;
        [SerializeField] private bool showHud;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.62f);
        [SerializeField] private Color textColor = Color.white;

        private PetsPerspectiveMode currentMode;
        private GUIStyle labelStyle;
        private Texture2D panelTexture;

        public PetsLikePlayerController Player
        {
            get => player;
            set => player = value;
        }

        private void Awake()
        {
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, panelColor);
            panelTexture.Apply();
        }

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }

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

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
        }

        private string ModeName()
        {
            return currentMode == PetsPerspectiveMode.TwoD ? "2D line platform" : "2.5D angled platform";
        }
    }
}
