using DimensionShift.PetsLike;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DimensionShiftEditor
{
    [CustomEditor(typeof(PetsSceneMapPreview))]
    public sealed class PetsSceneMapPreviewEditor : Editor
    {
        private const string RenderOutputFolder = "Assets/DimensionShift/GeneratedPreviews";
        private const int RenderWidth = 2048;
        private const int RenderHeight = 1536;
        private const float RenderPadding = 1.12f;

        private static PetsCellKind brush = PetsCellKind.WhiteInterior;

        private PetsSceneMapPreview Preview => (PetsSceneMapPreview)target;

        [MenuItem("Tools/Dimension Shift/Render Scene Map Preview/Current PNG")]
        public static void RenderActiveScenePreviewCurrentPng()
        {
            PetsSceneMapPreview preview = FindOrCreateActiveScenePreview();
            if (preview != null)
            {
                RenderGeneratedPreviewPng(preview, preview.GeneratedPreviewMode);
            }
        }

        [MenuItem("Tools/Dimension Shift/Render Scene Map Preview/2D PNG")]
        public static void RenderActiveScenePreview2DPng()
        {
            PetsSceneMapPreview preview = FindOrCreateActiveScenePreview();
            if (preview != null)
            {
                RenderGeneratedPreviewPng(preview, PetsPerspectiveMode.TwoD);
            }
        }

        [MenuItem("Tools/Dimension Shift/Render Scene Map Preview/2.5D PNG")]
        public static void RenderActiveScenePreviewTwoPointFiveDPng()
        {
            PetsSceneMapPreview preview = FindOrCreateActiveScenePreview();
            if (preview != null)
            {
                RenderGeneratedPreviewPng(preview, PetsPerspectiveMode.TwoPointFiveD);
            }
        }

        [MenuItem("Tools/Dimension Shift/Render Scene Map Preview/Both 2D and 2.5D PNGs")]
        public static void RenderActiveScenePreviewPngs()
        {
            PetsSceneMapPreview preview = FindOrCreateActiveScenePreview();
            if (preview == null)
            {
                return;
            }

            RenderGeneratedPreviewPng(preview, PetsPerspectiveMode.TwoD);
            RenderGeneratedPreviewPng(preview, PetsPerspectiveMode.TwoPointFiveD);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
            EditorApplication.delayCall += RebuildPreviewOnSelection;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Preview);
                RefreshGeneratedPreview();
            }

            EditorGUILayout.Space();
            DrawGeneratedPreviewToolbar();

            EditorGUILayout.Space();
            DrawBrushToolbar();

            if (GUILayout.Button("Use Map From Scene Bootstrap"))
            {
                if (DimensionPrototypeSceneBuilder.TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset level))
                {
                    Undo.RecordObject(Preview, "Use Scene PETS Map");
                    Preview.EditableLevel = level;
                    Preview.RebuildGeneratedPreview();
                    EditorUtility.SetDirty(Preview);
                }
                else
                {
                    EditorUtility.DisplayDialog("PETS Scene Map Preview", "The active scene does not have a bound PETS editable map.", "OK");
                }
            }

            EditorGUILayout.HelpBox("Select this preview, then paint directly in the Scene view. Left-click or drag paints the selected brush. The Spawn brush moves the player start without changing terrain.", MessageType.Info);
        }

        private void DrawGeneratedPreviewToolbar()
        {
            EditorGUILayout.LabelField("Generated Full Map Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Rebuild"))
                {
                    Preview.RebuildGeneratedPreview();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Clear"))
                {
                    Preview.ClearGeneratedPreview();
                    SceneView.RepaintAll();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawGeneratedModeButton("2D", PetsPerspectiveMode.TwoD);
                DrawGeneratedModeButton("2.5D", PetsPerspectiveMode.TwoPointFiveD);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Render Current PNG"))
                {
                    RenderGeneratedPreviewPng(Preview, Preview.GeneratedPreviewMode);
                }

                if (GUILayout.Button("Render 2D PNG"))
                {
                    RenderGeneratedPreviewPng(Preview, PetsPerspectiveMode.TwoD);
                }

                if (GUILayout.Button("Render 2.5D PNG"))
                {
                    RenderGeneratedPreviewPng(Preview, PetsPerspectiveMode.TwoPointFiveD);
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGeneratedModeButton(string label, PetsPerspectiveMode mode)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = Preview.GeneratedPreviewMode == mode ? new Color(0.68f, 0.86f, 1f) : Color.white;
            if (GUILayout.Button(label))
            {
                Undo.RecordObject(Preview, "Set Generated PETS Preview Mode");
                Preview.SetGeneratedPreviewMode(mode);
                EditorUtility.SetDirty(Preview);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = previous;
        }

        private static void DrawBrushToolbar()
        {
            EditorGUILayout.LabelField("Scene Paint Brush", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawBrushButton("White", PetsCellKind.WhiteInterior);
                DrawBrushButton("Black", PetsCellKind.BlackRegion);
                DrawBrushButton("2.5D", PetsCellKind.SwitchToTwoPointFiveD);
                DrawBrushButton("2D", PetsCellKind.SwitchTo2D);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawBrushButton("Exit", PetsCellKind.Exit);
                DrawBrushButton("Brick", PetsCellKind.BreakableBrick);
                DrawBrushButton("Box", PetsCellKind.PushBox);
                DrawBrushButton("HeadBox", PetsCellKind.HeadBreakBox);
                DrawBrushButton("Star", PetsCellKind.Star);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawBrushButton("Bounce", PetsCellKind.BouncePad);
                DrawBrushButton("Erase", PetsCellKind.Empty);
                DrawBrushButton("Spawn", PetsCellKind.WhiteLine);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawBrushButton(string label, PetsCellKind kind)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = brush == kind ? new Color(0.68f, 0.86f, 1f) : Color.white;
            if (GUILayout.Button(label, GUILayout.MinWidth(54f)))
            {
                brush = kind;
            }

            GUI.backgroundColor = previous;
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            PetsSceneMapPreview preview = Preview;
            if (preview == null || preview.EditableLevel == null)
            {
                return;
            }

            if (!preview.ShowGeneratedPreview || preview.ShowGrid || preview.ShowCells)
            {
                DrawMap(preview);
            }

            HandlePaint(preview);
        }

        private static void DrawMap(PetsSceneMapPreview preview)
        {
            PetsEditableLevelAsset level = preview.EditableLevel;
            float cellSize = level.CellSize;
            Vector3 origin = preview.transform.position;

            if (preview.ShowCells)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    for (int x = 0; x < level.Width; x++)
                    {
                        PetsCellKind kind = level.GetCell(x, y);
                        PetsCellKind marker = level.GetMarker(x, y);
                        PetsPropKind prop = level.GetProp(x, y);
                        bool hasStar = level.HasStar(x, y);
                        DrawCell(preview, origin, cellSize, x, y, kind, marker, prop, hasStar);
                    }
                }

                DrawSpawn(preview, origin, cellSize);
            }

            if (preview.ShowGrid)
            {
                DrawGrid(preview, origin, cellSize);
            }
        }

        private static void DrawCell(PetsSceneMapPreview preview, Vector3 origin, float cellSize, int x, int y, PetsCellKind kind, PetsCellKind marker, PetsPropKind prop, bool hasStar)
        {
            Vector3 center = origin + new Vector3(x * cellSize, y * cellSize, 0f);
            Color color = kind == PetsCellKind.Empty ? preview.EmptyColor : ColorFor(preview, kind);
            Vector3 size = Vector3.one * cellSize * 0.92f;

            if (kind != PetsCellKind.Empty)
            {
                Handles.DrawSolidRectangleWithOutline(CellCorners(center, size, -0.055f), color, Color.clear);
            }

            if (marker != PetsCellKind.Empty)
            {
                Handles.DrawSolidRectangleWithOutline(CellCorners(center, Vector3.one * cellSize * 0.7f, -0.065f), ColorFor(preview, marker), Color.clear);
            }

            if (prop != PetsPropKind.None)
            {
                if (kind == PetsCellKind.Empty && marker == PetsCellKind.Empty)
                {
                    Handles.DrawSolidRectangleWithOutline(CellCorners(center, size, -0.055f), preview.WhiteColor, Color.clear);
                }

                Handles.DrawSolidRectangleWithOutline(CellCorners(center, Vector3.one * cellSize * 0.52f, -0.065f), ColorFor(preview, prop), Color.clear);
            }

            if (hasStar)
            {
                if (kind == PetsCellKind.Empty && marker == PetsCellKind.Empty && prop == PetsPropKind.None)
                {
                    Handles.DrawSolidRectangleWithOutline(CellCorners(center, size, -0.055f), preview.WhiteColor, Color.clear);
                }

                Vector3 starCenter = prop != PetsPropKind.None
                    ? center + new Vector3(cellSize * 0.27f, -cellSize * 0.27f, 0f)
                    : center;
                float starSize = prop != PetsPropKind.None ? 0.24f : 0.38f;
                Handles.DrawSolidRectangleWithOutline(CellCorners(starCenter, Vector3.one * cellSize * starSize, -0.075f), preview.StarColor, Color.clear);
            }

            if (preview.ShowLabels)
            {
                string label = prop != PetsPropKind.None ? LabelFor(prop) : hasStar ? "*" : marker != PetsCellKind.Empty ? LabelFor(marker) : LabelFor(kind);
                if (!string.IsNullOrEmpty(label))
                {
                    DrawLabel(center + Vector3.forward * -0.08f, label, kind == PetsCellKind.BlackRegion && marker == PetsCellKind.Empty ? Color.white : Color.black);
                }

                if (hasStar && prop != PetsPropKind.None)
                {
                    DrawLabel(center + new Vector3(cellSize * 0.27f, -cellSize * 0.27f, -0.09f), "*", Color.black);
                }
            }
        }

        private static void DrawSpawn(PetsSceneMapPreview preview, Vector3 origin, float cellSize)
        {
            PetsGridCoord spawn = preview.EditableLevel.Spawn;
            Vector3 center = origin + new Vector3(spawn.x * cellSize, spawn.y * cellSize, 0f);
            Handles.DrawSolidRectangleWithOutline(CellCorners(center, Vector3.one * cellSize * 0.34f, -0.09f), preview.SpawnColor, Color.clear);
            DrawLabel(center + Vector3.forward * -0.02f, "S", Color.white);
        }

        private static void DrawGrid(PetsSceneMapPreview preview, Vector3 origin, float cellSize)
        {
            PetsEditableLevelAsset level = preview.EditableLevel;
            Handles.color = preview.GridColor;

            float minX = origin.x - cellSize * 0.5f;
            float maxX = origin.x + (level.Width - 0.5f) * cellSize;
            float minY = origin.y - cellSize * 0.5f;
            float maxY = origin.y + (level.Height - 0.5f) * cellSize;

            for (int x = 0; x <= level.Width; x++)
            {
                float lineX = minX + x * cellSize;
                Handles.DrawLine(new Vector3(lineX, minY, origin.z), new Vector3(lineX, maxY, origin.z));
            }

            for (int y = 0; y <= level.Height; y++)
            {
                float lineY = minY + y * cellSize;
                Handles.DrawLine(new Vector3(minX, lineY, origin.z), new Vector3(maxX, lineY, origin.z));
            }
        }

        private static Vector3[] CellCorners(Vector3 center, Vector3 size, float z)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            return new[]
            {
                new Vector3(center.x - halfX, center.y - halfY, center.z + z),
                new Vector3(center.x - halfX, center.y + halfY, center.z + z),
                new Vector3(center.x + halfX, center.y + halfY, center.z + z),
                new Vector3(center.x + halfX, center.y - halfY, center.z + z)
            };
        }

        private static void DrawLabel(Vector3 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color },
                fontSize = 11
            };
            Handles.Label(position, text, style);
        }

        private static void HandlePaint(PetsSceneMapPreview preview)
        {
            Event evt = Event.current;
            if (evt.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            if ((evt.type != EventType.MouseDown && evt.type != EventType.MouseDrag) || evt.button != 0 || evt.alt)
            {
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            Plane plane = new Plane(Vector3.forward, preview.transform.position);
            if (!plane.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 local = ray.GetPoint(distance) - preview.transform.position;
            PetsEditableLevelAsset level = preview.EditableLevel;
            int x = Mathf.RoundToInt(local.x / level.CellSize);
            int y = Mathf.RoundToInt(local.y / level.CellSize);
            if (x < 0 || y < 0 || x >= level.Width || y >= level.Height)
            {
                return;
            }

            Undo.RecordObject(level, "Paint PETS Scene Map");
            if (brush == PetsCellKind.WhiteLine)
            {
                level.SetSpawn(new PetsGridCoord(x, y));
            }
            else
            {
                PaintBrush(level, x, y);
            }

            EditorUtility.SetDirty(level);
            if (preview.ShowGeneratedPreview && preview.RebuildGeneratedPreviewAfterPaint)
            {
                preview.RebuildGeneratedPreview();
            }

            evt.Use();
            SceneView.RepaintAll();
        }

        private static Color ColorFor(PetsSceneMapPreview preview, PetsCellKind kind)
        {
            switch (kind)
            {
                case PetsCellKind.BlackRegion:
                    return preview.BlackColor;
                case PetsCellKind.SwitchToTwoPointFiveD:
                    return preview.SwitchTo25DColor;
                case PetsCellKind.SwitchTo2D:
                    return preview.SwitchTo2DColor;
                case PetsCellKind.Exit:
                    return preview.ExitColor;
                case PetsCellKind.BreakableBrick:
                    return preview.BrickColor;
                case PetsCellKind.PushBox:
                    return preview.BoxColor;
                case PetsCellKind.HeadBreakBox:
                    return preview.HeadBreakBoxColor;
                case PetsCellKind.Star:
                    return preview.StarColor;
                case PetsCellKind.BouncePad:
                    return preview.BouncePadColor;
                default:
                    return preview.WhiteColor;
            }
        }

        private static Color ColorFor(PetsSceneMapPreview preview, PetsPropKind kind)
        {
            switch (kind)
            {
                case PetsPropKind.BreakableBrick:
                    return preview.BrickColor;
                case PetsPropKind.PushBox:
                    return preview.BoxColor;
                case PetsPropKind.HeadBreakBox:
                    return preview.HeadBreakBoxColor;
                case PetsPropKind.Star:
                    return preview.StarColor;
                default:
                    return preview.WhiteColor;
            }
        }

        private static string LabelFor(PetsCellKind kind)
        {
            switch (kind)
            {
                case PetsCellKind.BlackRegion:
                    return "B";
                case PetsCellKind.SwitchToTwoPointFiveD:
                    return "2.5D";
                case PetsCellKind.SwitchTo2D:
                    return "2D";
                case PetsCellKind.Exit:
                    return "EXIT";
                case PetsCellKind.BreakableBrick:
                    return "BR";
                case PetsCellKind.PushBox:
                    return "BOX";
                case PetsCellKind.HeadBreakBox:
                    return "HEAD";
                case PetsCellKind.Star:
                    return "*";
                case PetsCellKind.BouncePad:
                    return "UP";
                default:
                    return string.Empty;
            }
        }

        private static string LabelFor(PetsPropKind kind)
        {
            switch (kind)
            {
                case PetsPropKind.BreakableBrick:
                    return "BR";
                case PetsPropKind.PushBox:
                    return "BOX";
                case PetsPropKind.HeadBreakBox:
                    return "HEAD";
                case PetsPropKind.Star:
                    return "*";
                default:
                    return string.Empty;
            }
        }

        private static void PaintBrush(PetsEditableLevelAsset level, int x, int y)
        {
            if (brush == PetsCellKind.Empty)
            {
                level.SetCell(x, y, PetsCellKind.Empty);
                level.SetProp(x, y, PetsPropKind.None);
                level.SetStar(x, y, false);
                return;
            }

            if (brush == PetsCellKind.Star)
            {
                level.SetStar(x, y, true);
                return;
            }

            if (TryBrushToProp(brush, out PetsPropKind prop))
            {
                level.SetProp(x, y, prop);
                if (level.GetCell(x, y) == PetsCellKind.Empty)
                {
                    level.SetCell(x, y, PetsCellKind.WhiteInterior);
                }

                return;
            }

            if (IsMarkerBrush(brush))
            {
                level.SetMarker(x, y, brush);
                return;
            }

            level.SetCell(x, y, brush);
        }

        private static bool TryBrushToProp(PetsCellKind brushKind, out PetsPropKind prop)
        {
            switch (brushKind)
            {
                case PetsCellKind.BreakableBrick:
                    prop = PetsPropKind.BreakableBrick;
                    return true;
                case PetsCellKind.PushBox:
                    prop = PetsPropKind.PushBox;
                    return true;
                case PetsCellKind.HeadBreakBox:
                    prop = PetsPropKind.HeadBreakBox;
                    return true;
                default:
                    prop = PetsPropKind.None;
                    return false;
            }
        }

        private static bool IsMarkerBrush(PetsCellKind brushKind)
        {
            return brushKind == PetsCellKind.SwitchTo2D
                || brushKind == PetsCellKind.SwitchToTwoPointFiveD
                || brushKind == PetsCellKind.Exit;
        }

        private void RefreshGeneratedPreview()
        {
            if (Preview.ShowGeneratedPreview)
            {
                Preview.RebuildGeneratedPreview();
            }
            else
            {
                Preview.ClearGeneratedPreview();
            }

            SceneView.RepaintAll();
        }

        private void RebuildPreviewOnSelection()
        {
            if (Preview == null || Preview.EditableLevel == null || !Preview.ShowGeneratedPreview)
            {
                return;
            }

            Preview.RebuildGeneratedPreview();
            SceneView.RepaintAll();
        }

        private static void RenderGeneratedPreviewPng(PetsSceneMapPreview preview, PetsPerspectiveMode mode)
        {
            if (preview == null || preview.EditableLevel == null)
            {
                NotifyRenderIssue("Render PETS Map", "Assign a PETS editable level before rendering.");
                return;
            }

            if (!preview.ShowGeneratedPreview)
            {
                NotifyRenderIssue("Render PETS Map", "Enable Show Generated Preview before rendering.");
                return;
            }

            Undo.RecordObject(preview, "Render PETS Generated Preview");
            preview.SetGeneratedPreviewMode(mode);
            preview.RebuildGeneratedPreview();

            Transform root = preview.GeneratedPreviewRoot;
            if (root == null || !TryCalculateRendererBounds(root, out Bounds bounds))
            {
                NotifyRenderIssue("Render PETS Map", "No generated preview renderers were found. Rebuild the generated preview and try again.");
                return;
            }

            byte[] png = RenderBoundsToPng(bounds, mode, RenderWidth, RenderHeight);
            EnsureRenderOutputFolder();
            string assetPath = $"{RenderOutputFolder}/{SanitizeAssetName(preview.EditableLevel.name)}_{ModeSuffix(mode)}_GeneratedPreview.png";
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            File.WriteAllBytes(fullPath, png);
            AssetDatabase.ImportAsset(assetPath);

            Object renderedAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            Selection.activeObject = renderedAsset;
            EditorGUIUtility.PingObject(renderedAsset);
            Debug.Log($"Rendered PETS generated map preview to {assetPath}");
        }

        private static PetsSceneMapPreview FindOrCreateActiveScenePreview()
        {
            PetsSceneMapPreview preview = FindActiveScenePreview();
            if (preview == null)
            {
                DimensionPrototypeSceneBuilder.EnsureSceneMapPreviewMenu();
                preview = FindActiveScenePreview();
            }

            if (preview == null)
            {
                NotifyRenderIssue("Render PETS Map", "The active scene does not have a PETS scene map preview.");
                return null;
            }

            if (preview.EditableLevel == null && DimensionPrototypeSceneBuilder.TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset editableLevel))
            {
                Undo.RecordObject(preview, "Assign PETS Scene Map Preview");
                preview.EditableLevel = editableLevel;
                EditorUtility.SetDirty(preview);
            }

            return preview;
        }

        private static PetsSceneMapPreview FindActiveScenePreview()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                PetsSceneMapPreview preview = roots[i].GetComponentInChildren<PetsSceneMapPreview>(true);
                if (preview != null)
                {
                    return preview;
                }
            }

            return null;
        }

        private static void NotifyRenderIssue(string title, string message)
        {
            if (Application.isBatchMode)
            {
                Debug.LogError($"{title}: {message}");
                return;
            }

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static byte[] RenderBoundsToPng(Bounds bounds, PetsPerspectiveMode mode, int width, int height)
        {
            GameObject cameraObject = new GameObject("PETS Generated Preview Render Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("PETS Generated Preview Render Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            RenderTexture renderTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            Camera camera = null;
            Texture2D image = null;

            try
            {
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.98f, 0.98f, 0.96f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 2000f;

                camera.transform.rotation = mode == PetsPerspectiveMode.TwoD
                    ? Quaternion.identity
                    : Quaternion.Euler(55f, 0f, 0f);
                FitCameraToBounds(camera, bounds, (float)width / height);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.95f;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4
                };
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();

                image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return image.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (image != null)
                {
                    Object.DestroyImmediate(image);
                }

                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FitCameraToBounds(Camera camera, Bounds bounds, float aspect)
        {
            Quaternion inverseRotation = Quaternion.Inverse(camera.transform.rotation);
            Vector3[] corners = BoundsCorners(bounds);
            float maxX = 0f;
            float maxY = 0f;
            float maxZ = 0f;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = inverseRotation * (corners[i] - bounds.center);
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
                maxZ = Mathf.Max(maxZ, Mathf.Abs(local.z));
            }

            camera.orthographicSize = Mathf.Max(maxY, maxX / Mathf.Max(0.001f, aspect), 0.5f) * RenderPadding;
            float distance = Mathf.Max(20f, maxZ + Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) + 8f);
            camera.transform.position = bounds.center - camera.transform.forward * distance;
            camera.farClipPlane = distance + maxZ + 100f;
        }

        private static bool TryCalculateRendererBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static Vector3[] BoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        private static void EnsureRenderOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DimensionShift"))
            {
                AssetDatabase.CreateFolder("Assets", "DimensionShift");
            }

            if (!AssetDatabase.IsValidFolder(RenderOutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/DimensionShift", "GeneratedPreviews");
            }
        }

        private static string SanitizeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "PETS_Map";
            }

            string sanitized = value.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                sanitized = sanitized.Replace(invalidCharacters[i], '_');
            }

            return sanitized.Replace(' ', '_');
        }

        private static string ModeSuffix(PetsPerspectiveMode mode)
        {
            return mode == PetsPerspectiveMode.TwoD ? "2D" : "2_5D";
        }
    }
}
