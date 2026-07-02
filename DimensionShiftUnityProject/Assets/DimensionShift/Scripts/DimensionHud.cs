using UnityEngine;

namespace DimensionShift
{
    public sealed class DimensionHud : DimensionListenerBehaviour
    {
        [SerializeField] private int fontSize = 18;
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.58f);
        [SerializeField] private Color textColor = Color.white;

        private DimensionMode currentMode;
        private GUIStyle labelStyle;
        private Texture2D panelTexture;

        private void Awake()
        {
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, panelColor);
            panelTexture.Apply();
        }

        private void OnGUI()
        {
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

            GUI.DrawTexture(new Rect(18, 18, 500, 146), panelTexture);
            GUI.Label(
                new Rect(18, 18, 500, 146),
                $"<b>Dimension Shift Prototype</b>\nMode: <b>{ModeName()}</b>\nTab: switch 2D / 3D    Space: jump    R: reset\n2D: A/D + jump, Z locked. 3D: WASD on ground plane.\nPush the orange crate onto the blue plate in 3D to open the door.",
                labelStyle);
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;
        }

        private string ModeName()
        {
            return currentMode == DimensionMode.TwoD ? "2D side slice" : "3D ground plane";
        }
    }
}
