using System;
using DimensionShift.PetsLike;
using UnityEditor;
using UnityEngine;

namespace DimensionShiftEditor
{
    public sealed class PetsLevelPainterWindow : EditorWindow
    {
        private const float CellPixels = 24f;
        private static readonly Color GridColor = new Color(0.62f, 0.62f, 0.56f);
        private static readonly Color PaperColor = new Color(0.9f, 0.9f, 0.86f);
        private static readonly Color WhiteCellColor = new Color(0.86f, 0.94f, 1f);
        private static readonly Color BlackCellColor = new Color(0.03f, 0.03f, 0.03f);
        private static readonly Color Switch25DColor = new Color(0.12f, 0.52f, 1f);
        private static readonly Color Switch2DColor = new Color(0.15f, 0.85f, 0.38f);
        private static readonly Color ExitColor = new Color(1f, 0.45f, 0.12f);
        private static readonly Color BrickColor = new Color(0.78f, 0.25f, 0.18f);
        private static readonly Color BoxColor = new Color(0.72f, 0.48f, 0.22f);
        private static readonly Color HeadBreakBoxColor = new Color(0.62f, 0.34f, 0.82f);
        private static readonly Color BouncePadColor = new Color(1f, 0.82f, 0.16f);
        private static readonly Color StarColor = new Color(1f, 0.86f, 0.18f);
        private static readonly Color SpawnColor = new Color(0.28f, 0.36f, 1f, 0.9f);

        private PetsEditableLevelAsset levelAsset;
        private PetsCellKind brush = PetsCellKind.WhiteInterior;
        private Vector2 scroll;
        private int width = 40;
        private int height = 16;
        private float cellSize = 1.15f;
        private Action queuedEditorAction;
        private PetsEditableLevelAsset mapSettingsSource;

        [MenuItem("Tools/Dimension Shift/PETS Level Painter")]
        public static void Open()
        {
            GetWindow<PetsLevelPainterWindow>("PETS Level Painter");
        }

        private void OnGUI()
        {
            DrawAssetToolbar();
            ProcessQueuedEditorAction();
            if (levelAsset == null)
            {
                EditorGUILayout.HelpBox("Create or assign a PETS Editable Level asset, then paint the 2D map. The runtime will flatten the same map into 2.5D automatically.", MessageType.Info);
                return;
            }

            DrawMapSettings();
            DrawBrushToolbar();
            DrawCanvas();
            DrawActions();
            ProcessQueuedEditorAction();
        }

        private void DrawAssetToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            try
            {
                PetsEditableLevelAsset selectedAsset = (PetsEditableLevelAsset)EditorGUILayout.ObjectField(levelAsset, typeof(PetsEditableLevelAsset), false, GUILayout.MinWidth(220f));
                if (selectedAsset != levelAsset)
                {
                    levelAsset = selectedAsset;
                    SyncMapSettingsFromAsset();
                }

                if (GUILayout.Button("Use Scene Map", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    QueueEditorAction(UseCurrentSceneMap);
                }

                if (GUILayout.Button("New For Scene", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    QueueEditorAction(CreateAndBindSceneAsset);
                }

                if (GUILayout.Button("Bind To Scene", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    QueueEditorAction(BindCurrentAssetToScene);
                }

                if (GUILayout.Button("New Asset", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    QueueEditorAction(CreateAsset);
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                {
                    QueueEditorAction(SaveAsset);
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawMapSettings()
        {
            SyncMapSettingsFromAssetIfNeeded();
            EditorGUILayout.BeginHorizontal();
            try
            {
                width = EditorGUILayout.IntField("Width", width, GUILayout.MaxWidth(180f));
                height = EditorGUILayout.IntField("Height", height, GUILayout.MaxWidth(180f));
                cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, GUILayout.MaxWidth(200f));
                if (GUILayout.Button("Apply Size", GUILayout.Width(92f)))
                {
                    width = Mathf.Max(1, width);
                    height = Mathf.Max(1, height);
                    cellSize = Mathf.Max(0.1f, cellSize);
                    Undo.RecordObject(levelAsset, "Resize PETS Level");
                    levelAsset.Resize(width, height);
                    levelAsset.SetCellSize(cellSize);
                    SyncMapSettingsFromAsset();
                    MarkDirty();
                    SceneView.RepaintAll();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBrushToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawBrushButton("White", PetsCellKind.WhiteInterior);
                DrawBrushButton("Black", PetsCellKind.BlackRegion);
                DrawBrushButton("2.5D", PetsCellKind.SwitchToTwoPointFiveD);
                DrawBrushButton("2D", PetsCellKind.SwitchTo2D);
                DrawBrushButton("Exit", PetsCellKind.Exit);
                DrawBrushButton("Brick", PetsCellKind.BreakableBrick);
                DrawBrushButton("Box", PetsCellKind.PushBox);
                DrawBrushButton("HeadBox", PetsCellKind.HeadBreakBox);
                DrawBrushButton("Star", PetsCellKind.Star);
                DrawBrushButton("Bounce", PetsCellKind.BouncePad);
                DrawBrushButton("Erase", PetsCellKind.Empty);
                if (GUILayout.Button("Spawn", GUILayout.Width(70f)))
                {
                    brush = PetsCellKind.WhiteLine;
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox("Left-click or drag to paint. The Spawn brush moves the player start without changing terrain.", MessageType.None);
        }

        private void DrawBrushButton(string label, PetsCellKind kind)
        {
            bool selected = brush == kind;
            GUIStyle style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.68f, 0.86f, 1f) : Color.white;
            if (GUILayout.Button(label, style, GUILayout.Width(70f)))
            {
                brush = kind;
            }

            GUI.backgroundColor = oldColor;
        }

        private void DrawCanvas()
        {
            float canvasWidth = levelAsset.Width * CellPixels;
            float canvasHeight = levelAsset.Height * CellPixels;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            try
            {
                Rect canvas = GUILayoutUtility.GetRect(canvasWidth, canvasHeight);
                EditorGUI.DrawRect(canvas, PaperColor);

                Event evt = Event.current;
                for (int y = 0; y < levelAsset.Height; y++)
                {
                    for (int x = 0; x < levelAsset.Width; x++)
                    {
                        Rect cellRect = CellRect(canvas, x, y);
                        PetsCellKind kind = levelAsset.GetCell(x, y);
                        PetsCellKind marker = levelAsset.GetMarker(x, y);
                        PetsPropKind prop = levelAsset.GetProp(x, y);
                        DrawCell(cellRect, kind, marker, prop);
                        Handles.color = GridColor;
                        Handles.DrawAAPolyLine(1f,
                            new Vector3(cellRect.xMin, cellRect.yMin),
                            new Vector3(cellRect.xMax, cellRect.yMin),
                            new Vector3(cellRect.xMax, cellRect.yMax),
                            new Vector3(cellRect.xMin, cellRect.yMax),
                            new Vector3(cellRect.xMin, cellRect.yMin));
                    }
                }

                DrawSpawn(canvas);
                HandlePaint(evt, canvas);
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private Rect CellRect(Rect canvas, int x, int y)
        {
            float drawY = levelAsset.Height - 1 - y;
            return new Rect(canvas.x + x * CellPixels, canvas.y + drawY * CellPixels, CellPixels, CellPixels);
        }

        private void DrawCell(Rect rect, PetsCellKind kind, PetsCellKind marker, PetsPropKind prop)
        {
            if (kind == PetsCellKind.Empty && marker == PetsCellKind.Empty && prop == PetsPropKind.None)
            {
                return;
            }

            Rect fillRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            Rect labelRect = rect;
            if (kind != PetsCellKind.Empty)
            {
                EditorGUI.DrawRect(fillRect, ColorFor(kind));
            }

            if (marker != PetsCellKind.Empty)
            {
                Rect markerRect = new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.12f, rect.width * 0.76f, rect.height * 0.76f);
                EditorGUI.DrawRect(markerRect, ColorFor(marker));
                labelRect = markerRect;
            }

            if (prop != PetsPropKind.None)
            {
                if (kind == PetsCellKind.Empty && marker == PetsCellKind.Empty)
                {
                    EditorGUI.DrawRect(fillRect, WhiteCellColor);
                }

                Rect propRect = new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.22f, rect.width * 0.56f, rect.height * 0.56f);
                EditorGUI.DrawRect(propRect, ColorFor(prop));
                labelRect = propRect;
            }

            string label = prop != PetsPropKind.None ? LabelFor(prop) : marker != PetsCellKind.Empty ? LabelFor(marker) : LabelFor(kind);
            if (!string.IsNullOrEmpty(label))
            {
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = kind == PetsCellKind.BlackRegion && marker == PetsCellKind.Empty ? Color.white : Color.black },
                    fontSize = prop == PetsPropKind.PushBox || prop == PetsPropKind.HeadBreakBox ? 8 : 10
                };
                GUI.Label(labelRect, label, style);
            }
        }

        private void DrawSpawn(Rect canvas)
        {
            PetsGridCoord spawn = levelAsset.Spawn;
            Rect rect = CellRect(canvas, spawn.x, spawn.y);
            EditorGUI.DrawRect(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), SpawnColor);
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 9
            };
            GUI.Label(rect, "S", style);
        }

        private void HandlePaint(Event evt, Rect canvas)
        {
            if ((evt.type != EventType.MouseDown && evt.type != EventType.MouseDrag) || evt.button != 0 || !canvas.Contains(evt.mousePosition))
            {
                return;
            }

            int x = Mathf.FloorToInt((evt.mousePosition.x - canvas.x) / CellPixels);
            int drawY = Mathf.FloorToInt((evt.mousePosition.y - canvas.y) / CellPixels);
            int y = levelAsset.Height - 1 - drawY;
            if (x < 0 || y < 0 || x >= levelAsset.Width || y >= levelAsset.Height)
            {
                return;
            }

            Undo.RecordObject(levelAsset, "Paint PETS Level");
            if (brush == PetsCellKind.WhiteLine)
            {
                levelAsset.SetSpawn(new PetsGridCoord(x, y));
            }
            else
            {
                PaintBrush(x, y);
            }

            MarkDirty();
            evt.Use();
            Repaint();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Clear Map"))
                {
                    Undo.RecordObject(levelAsset, "Clear PETS Level");
                    levelAsset.Clear();
                    MarkDirty();
                }

                if (GUILayout.Button("Generate Test Scene"))
                {
                    PetsEditableLevelAsset assetToBuild = levelAsset;
                    QueueEditorAction(() => DimensionPrototypeSceneBuilder.CreateEditablePainterTestScene(assetToBuild));
                }

                if (GUILayout.Button("Show In Scene"))
                {
                    QueueEditorAction(DimensionPrototypeSceneBuilder.EnsureSceneMapPreviewForCurrentScene);
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create PETS Editable Level", "PETS_Editable_Level", "asset", "Choose where to save the editable level asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            PetsEditableLevelAsset asset = CreateInstance<PetsEditableLevelAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            levelAsset = asset;
            SyncMapSettingsFromAsset();
            Selection.activeObject = asset;
        }

        private void UseCurrentSceneMap()
        {
            if (DimensionPrototypeSceneBuilder.TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset sceneLevel))
            {
                levelAsset = sceneLevel;
                SyncMapSettingsFromAsset();
                Selection.activeObject = sceneLevel;
                return;
            }

            EditorUtility.DisplayDialog("Use Scene Map", "The active scene does not have a PETS editable map bound yet. Use New For Scene to create and bind one.", "OK");
        }

        private void CreateAndBindSceneAsset()
        {
            levelAsset = DimensionPrototypeSceneBuilder.CreateAndBindEditableLevelToCurrentScene();
            SyncMapSettingsFromAsset();
            Selection.activeObject = levelAsset;
        }

        private void BindCurrentAssetToScene()
        {
            if (levelAsset == null)
            {
                EditorUtility.DisplayDialog("Bind To Scene", "Create or assign a PETS Editable Level asset first.", "OK");
                return;
            }

            DimensionPrototypeSceneBuilder.BindEditableLevelToCurrentScene(levelAsset);
        }

        private void SaveAsset()
        {
            if (levelAsset == null)
            {
                return;
            }

            MarkDirty();
            AssetDatabase.SaveAssets();
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(levelAsset);
        }

        private void SyncMapSettingsFromAssetIfNeeded()
        {
            if (mapSettingsSource == levelAsset)
            {
                return;
            }

            SyncMapSettingsFromAsset();
        }

        private void SyncMapSettingsFromAsset()
        {
            mapSettingsSource = levelAsset;
            if (levelAsset == null)
            {
                return;
            }

            width = levelAsset.Width;
            height = levelAsset.Height;
            cellSize = levelAsset.CellSize;
        }

        private void QueueEditorAction(Action action)
        {
            queuedEditorAction = action;
        }

        private void ProcessQueuedEditorAction()
        {
            if (queuedEditorAction == null)
            {
                return;
            }

            Action action = queuedEditorAction;
            queuedEditorAction = null;
            EditorApplication.delayCall += () =>
            {
                action();
                if (this != null)
                {
                    Repaint();
                }
            };
            GUIUtility.ExitGUI();
        }

        private static Color ColorFor(PetsCellKind kind)
        {
            switch (kind)
            {
                case PetsCellKind.BlackRegion:
                    return BlackCellColor;
                case PetsCellKind.SwitchToTwoPointFiveD:
                    return Switch25DColor;
                case PetsCellKind.SwitchTo2D:
                    return Switch2DColor;
                case PetsCellKind.Exit:
                    return ExitColor;
                case PetsCellKind.BreakableBrick:
                    return BrickColor;
                case PetsCellKind.PushBox:
                    return BoxColor;
                case PetsCellKind.HeadBreakBox:
                    return HeadBreakBoxColor;
                case PetsCellKind.Star:
                    return StarColor;
                case PetsCellKind.BouncePad:
                    return BouncePadColor;
                default:
                    return WhiteCellColor;
            }
        }

        private static Color ColorFor(PetsPropKind kind)
        {
            switch (kind)
            {
                case PetsPropKind.BreakableBrick:
                    return BrickColor;
                case PetsPropKind.PushBox:
                    return BoxColor;
                case PetsPropKind.HeadBreakBox:
                    return HeadBreakBoxColor;
                case PetsPropKind.Star:
                    return StarColor;
                default:
                    return WhiteCellColor;
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

        private void PaintBrush(int x, int y)
        {
            if (brush == PetsCellKind.Empty)
            {
                levelAsset.SetCell(x, y, PetsCellKind.Empty);
                levelAsset.SetProp(x, y, PetsPropKind.None);
                return;
            }

            if (TryBrushToProp(brush, out PetsPropKind prop))
            {
                levelAsset.SetProp(x, y, prop);
                if (levelAsset.GetCell(x, y) == PetsCellKind.Empty)
                {
                    levelAsset.SetCell(x, y, PetsCellKind.WhiteInterior);
                }

                return;
            }

            if (IsMarkerBrush(brush))
            {
                levelAsset.SetMarker(x, y, brush);
                return;
            }

            levelAsset.SetCell(x, y, brush);
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
                case PetsCellKind.Star:
                    prop = PetsPropKind.Star;
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
    }
}
