using UnityEngine;

namespace DimensionShift.PetsLike
{
    [ExecuteAlways]
    public sealed class PetsSceneMapPreview : MonoBehaviour
    {
        [SerializeField] private PetsEditableLevelAsset editableLevel;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showCells = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField] private Color gridColor = new Color(0f, 0f, 0f, 0.18f);
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color whiteColor = new Color(1f, 1f, 1f, 0.65f);
        [SerializeField] private Color blackColor = new Color(0f, 0f, 0f, 0.78f);
        [SerializeField] private Color switchTo25DColor = new Color(0.12f, 0.52f, 1f, 0.72f);
        [SerializeField] private Color switchTo2DColor = new Color(0.15f, 0.85f, 0.38f, 0.72f);
        [SerializeField] private Color exitColor = new Color(1f, 0.45f, 0.12f, 0.72f);
        [SerializeField] private Color brickColor = new Color(0.78f, 0.25f, 0.18f, 0.82f);
        [SerializeField] private Color boxColor = new Color(0.72f, 0.48f, 0.22f, 0.82f);
        [SerializeField] private Color headBreakBoxColor = new Color(0.62f, 0.34f, 0.82f, 0.82f);
        [SerializeField] private Color bouncePadColor = new Color(1f, 0.82f, 0.16f, 0.82f);
        [SerializeField] private Color starColor = new Color(1f, 0.86f, 0.18f, 0.86f);
        [SerializeField] private Color spawnColor = new Color(0.28f, 0.36f, 1f, 0.86f);

        public PetsEditableLevelAsset EditableLevel
        {
            get => editableLevel;
            set => editableLevel = value;
        }

        public bool ShowGrid => showGrid;
        public bool ShowCells => showCells;
        public bool ShowLabels => showLabels;

        public Color GridColor => gridColor;
        public Color EmptyColor => emptyColor;
        public Color WhiteColor => whiteColor;
        public Color BlackColor => blackColor;
        public Color SwitchTo25DColor => switchTo25DColor;
        public Color SwitchTo2DColor => switchTo2DColor;
        public Color ExitColor => exitColor;
        public Color BrickColor => brickColor;
        public Color BoxColor => boxColor;
        public Color HeadBreakBoxColor => headBreakBoxColor;
        public Color BouncePadColor => bouncePadColor;
        public Color StarColor => starColor;
        public Color SpawnColor => spawnColor;

        private void Reset()
        {
            global::DimensionShift.DimensionPrototypeBootstrap bootstrap = GetComponentInParent<global::DimensionShift.DimensionPrototypeBootstrap>();
            if (bootstrap != null)
            {
                editableLevel = bootstrap.EditableLevel;
            }
        }
    }
}
