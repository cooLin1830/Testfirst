using UnityEngine;

namespace DimensionShift.PetsLike
{
    [ExecuteAlways]
    public sealed class PetsSceneMapPreview : MonoBehaviour
    {
        private const string GeneratedPreviewRootName = "Generated Map Full Preview";

        [SerializeField] private PetsEditableLevelAsset editableLevel;
        [SerializeField] private bool showGeneratedPreview = true;
        [SerializeField] private PetsPerspectiveMode generatedPreviewMode = PetsPerspectiveMode.TwoD;
        [SerializeField] private bool rebuildGeneratedPreviewAfterPaint = true;
        [SerializeField] private bool showGrid;
        [SerializeField] private bool showCells;
        [SerializeField] private bool showLabels;
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
        public bool ShowGeneratedPreview => showGeneratedPreview;
        public bool RebuildGeneratedPreviewAfterPaint => rebuildGeneratedPreviewAfterPaint;
        public PetsPerspectiveMode GeneratedPreviewMode => generatedPreviewMode;
        public Transform GeneratedPreviewRoot => FindGeneratedPreviewRoot();

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

        public void RebuildGeneratedPreview()
        {
            ClearGeneratedPreview();
            if (!showGeneratedPreview || editableLevel == null)
            {
                return;
            }

            GameObject previewRoot = new GameObject(GeneratedPreviewRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor
            };
            previewRoot.transform.SetParent(transform, false);
            previewRoot.transform.localPosition = Vector3.zero;
            previewRoot.transform.localRotation = Quaternion.identity;
            previewRoot.transform.localScale = Vector3.one;

            PetsLevelRuntime runtime = previewRoot.AddComponent<PetsLevelRuntime>();
            runtime.Build(
                editableLevel.ToLevelDefinition(),
                CreatePreviewMaterial("Preview Paper White", new Color(0.98f, 0.98f, 0.96f)),
                CreatePreviewMaterial("Preview Ink Black", Color.black),
                CreatePreviewMaterial("Preview Switch Tile", new Color(0.12f, 0.52f, 1f)),
                CreatePreviewMaterial("Preview Exit Tile", new Color(0.1f, 0.82f, 0.35f)),
                CreatePreviewMaterial("Preview Breakable Brick", new Color(0.72f, 0.22f, 0.16f)),
                CreatePreviewMaterial("Preview Push Box", new Color(0.68f, 0.43f, 0.18f)),
                CreatePreviewMaterial("Preview Bounce Pad", new Color(1f, 0.82f, 0.16f)));

            ApplyPreviewMode(previewRoot.transform);
            DisablePreviewColliders(previewRoot.transform);
            ApplyPreviewHideFlags(previewRoot.transform);
        }

        public void ClearGeneratedPreview()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name == GeneratedPreviewRootName)
                {
                    DestroyPreviewObject(child.gameObject);
                }
            }
        }

        public void SetGeneratedPreviewMode(PetsPerspectiveMode mode)
        {
            generatedPreviewMode = mode;
            Transform root = FindGeneratedPreviewRoot();
            if (root != null)
            {
                ApplyPreviewMode(root);
                DisablePreviewColliders(root);
            }
        }

        private Transform FindGeneratedPreviewRoot()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name == GeneratedPreviewRootName)
                {
                    return child;
                }
            }

            return null;
        }

        private void ApplyPreviewMode(Transform root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPetsPerspectiveListener listener)
                {
                    listener.SetPerspectiveMode(generatedPreviewMode);
                }
            }
        }

        private static void DisablePreviewColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void ApplyPreviewHideFlags(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
        }

        private static Material CreatePreviewMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color,
                hideFlags = HideFlags.DontSaveInEditor
            };
            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Glossiness", 0f);
            }

            return material;
        }

        private static void DestroyPreviewObject(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(item);
            }
            else
            {
                DestroyImmediate(item);
            }
        }
    }
}
