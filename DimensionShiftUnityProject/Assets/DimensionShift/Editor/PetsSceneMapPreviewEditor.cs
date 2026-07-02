using DimensionShift.PetsLike;
using UnityEditor;
using UnityEngine;

namespace DimensionShiftEditor
{
    [CustomEditor(typeof(PetsSceneMapPreview))]
    public sealed class PetsSceneMapPreviewEditor : Editor
    {
        private static PetsCellKind brush = PetsCellKind.WhiteInterior;

        private PetsSceneMapPreview Preview => (PetsSceneMapPreview)target;

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawBrushToolbar();

            if (GUILayout.Button("Use Map From Scene Bootstrap"))
            {
                if (DimensionPrototypeSceneBuilder.TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset level))
                {
                    Undo.RecordObject(Preview, "Use Scene PETS Map");
                    Preview.EditableLevel = level;
                    EditorUtility.SetDirty(Preview);
                }
                else
                {
                    EditorUtility.DisplayDialog("PETS Scene Map Preview", "The active scene does not have a bound PETS editable map.", "OK");
                }
            }

            EditorGUILayout.HelpBox("Select this preview, then paint directly in the Scene view. Left-click or drag paints the selected brush. The Spawn brush moves the player start without changing terrain.", MessageType.Info);
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

            DrawMap(preview);
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
                        PetsPropKind prop = level.GetProp(x, y);
                        DrawCell(preview, origin, cellSize, x, y, kind, prop);
                    }
                }

                DrawSpawn(preview, origin, cellSize);
            }

            if (preview.ShowGrid)
            {
                DrawGrid(preview, origin, cellSize);
            }
        }

        private static void DrawCell(PetsSceneMapPreview preview, Vector3 origin, float cellSize, int x, int y, PetsCellKind kind, PetsPropKind prop)
        {
            Vector3 center = origin + new Vector3(x * cellSize, y * cellSize, 0f);
            Color color = kind == PetsCellKind.Empty ? preview.EmptyColor : ColorFor(preview, kind);
            Vector3 size = Vector3.one * cellSize * 0.92f;

            if (prop != PetsPropKind.None)
            {
                Handles.DrawSolidRectangleWithOutline(CellCorners(center, size, -0.055f), kind == PetsCellKind.Empty ? preview.WhiteColor : color, Color.clear);
                Handles.DrawSolidRectangleWithOutline(CellCorners(center, Vector3.one * cellSize * 0.52f, -0.065f), ColorFor(preview, prop), Color.clear);
            }
            else if (kind != PetsCellKind.Empty)
            {
                Handles.DrawSolidRectangleWithOutline(CellCorners(center, size, -0.055f), color, Color.clear);
            }

            if (preview.ShowLabels)
            {
                string label = prop != PetsPropKind.None ? LabelFor(prop) : LabelFor(kind);
                if (!string.IsNullOrEmpty(label))
                {
                    DrawLabel(center + Vector3.forward * -0.08f, label, kind == PetsCellKind.BlackRegion ? Color.white : Color.black);
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
    }
}
