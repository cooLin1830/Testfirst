using System.Collections.Generic;
using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsLevelRuntime : PetsPerspectiveListenerBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float twoDLineThickness = 0.18f;
        [SerializeField] private float twoDLineDepth = 0.8f;
        [SerializeField] private float topDownPlatformThickness = 0.16f;
        [SerializeField] private float topDownHoleDepth = 0.28f;
        [SerializeField] private float inkLineWidth2D = 0.055f;
        [SerializeField] private float inkLineWidth25D = 0.045f;
        [SerializeField] private float inkWobble = 0.025f;
        [SerializeField] private Vector3 flattenedShadowOffset = new Vector3(0.08f, -0.045f, -0.12f);
        [SerializeField] private Color flattenedShadowColor = new Color(0.72f, 0.72f, 0.68f, 1f);

        private readonly Dictionary<Vector2Int, PetsCellKind> twoDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsCellKind> twoPointFiveDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsSwitchTile> switchTiles = new Dictionary<Vector2Int, PetsSwitchTile>();
        private readonly Dictionary<Vector2Int, GameObject> topDownPlatforms = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, PetsBreakableBrick> breakableBricks = new Dictionary<Vector2Int, PetsBreakableBrick>();
        private readonly Dictionary<Vector2Int, PetsPushBox> pushBoxes = new Dictionary<Vector2Int, PetsPushBox>();
        private readonly List<GameObject> sharedObjects = new List<GameObject>();
        private readonly List<GameObject> twoDObjects = new List<GameObject>();
        private readonly List<GameObject> topDownObjects = new List<GameObject>();

        private Transform twoDRoot;
        private Transform topDownRoot;
        private Transform sharedRoot;
        private Material flattenedShadowMaterial;
        private PetsPerspectiveMode currentMode;

        public PetsLevelDefinition Definition { get; private set; }
        public float CellSize => cellSize;

        public void Build(PetsLevelDefinition definition, Material whiteMaterial, Material blackMaterial, Material switchMaterial, Material exitMaterial, Material brickMaterial, Material boxMaterial)
        {
            Definition = definition;
            cellSize = definition.CellSize;
            twoDCells.Clear();
            twoPointFiveDCells.Clear();
            switchTiles.Clear();
            topDownPlatforms.Clear();
            breakableBricks.Clear();
            pushBoxes.Clear();
            twoDObjects.Clear();
            topDownObjects.Clear();
            sharedObjects.Clear();

            twoDRoot = CreateRoot("2D Mode Objects");
            topDownRoot = CreateRoot("2.5D Mode Objects");
            sharedRoot = CreateRoot("Shared Mode Objects");

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in definition.TwoDCells)
            {
                twoDCells[cell.Key] = cell.Value;
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in definition.TwoPointFiveDCells)
            {
                twoPointFiveDCells[cell.Key] = cell.Value;
            }

            HashSet<Vector2Int> markerCoords = new HashSet<Vector2Int>();

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoDCells)
            {
                BuildTwoDCell(cell.Key, cell.Value, blackMaterial, brickMaterial);
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoPointFiveDCells)
            {
                BuildTwoPointFiveDCell(cell.Key, cell.Value, whiteMaterial, blackMaterial, brickMaterial, boxMaterial);
            }

            BuildBlackRegionFills(twoDCells, PetsPerspectiveMode.TwoD, blackMaterial);
            BuildBlackRegionFills(twoPointFiveDCells, PetsPerspectiveMode.TwoPointFiveD, blackMaterial);
            BuildMergedTwoDShapeInk(blackMaterial);
            BuildMergedTopDownShapeInk(blackMaterial);

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoDCells)
            {
                if (IsMarkerKind(cell.Value))
                {
                    GameObject marker = CreateMarker(cell.Key, cell.Value, switchMaterial, exitMaterial);
                    marker.transform.SetParent(sharedRoot);
                    sharedObjects.Add(marker);
                    markerCoords.Add(cell.Key);
                }
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoPointFiveDCells)
            {
                if (markerCoords.Contains(cell.Key))
                {
                    continue;
                }

                if (IsMarkerKind(cell.Value))
                {
                    GameObject marker = CreateMarker(cell.Key, cell.Value, switchMaterial, exitMaterial);
                    marker.transform.SetParent(sharedRoot);
                    sharedObjects.Add(marker);
                    markerCoords.Add(cell.Key);
                }
            }

            BuildMergedBlackRegionInk(twoDCells, PetsPerspectiveMode.TwoD, blackMaterial);
            BuildMergedBlackRegionInk(twoPointFiveDCells, PetsPerspectiveMode.TwoPointFiveD, blackMaterial);
            SetPerspectiveMode(PetsModeManager.Instance != null ? PetsModeManager.Instance.CurrentMode : PetsPerspectiveMode.TwoD);
        }

        public Vector3 GridToTwoDWorld(PetsGridCoord coord, float z = 0f)
        {
            return new Vector3(coord.x * cellSize, coord.y * cellSize, z);
        }

        public Vector3 GridToTopDownWorld(PetsGridCoord coord, float y = 0f)
        {
            return new Vector3(coord.x * cellSize, y, coord.y * cellSize);
        }

        public PetsGridCoord WorldToGrid(Vector3 world, PetsPerspectiveMode mode)
        {
            if (mode == PetsPerspectiveMode.TwoD)
            {
                return new PetsGridCoord(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));
            }

            return new PetsGridCoord(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.z / cellSize));
        }

        public PetsCellKind GetCell(PetsGridCoord coord)
        {
            return GetCell(coord, currentMode);
        }

        public PetsCellKind GetCell(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            Dictionary<Vector2Int, PetsCellKind> source = mode == PetsPerspectiveMode.TwoPointFiveD ? twoPointFiveDCells : twoDCells;
            if (source.TryGetValue(coord.ToVector2Int(), out PetsCellKind kind))
            {
                return kind;
            }

            return PetsCellKind.Empty;
        }

        public bool CanSwitchAt(PetsGridCoord coord, PetsPerspectiveMode targetMode)
        {
            PetsCellKind kind = GetCell(coord, currentMode);
            return (targetMode == PetsPerspectiveMode.TwoPointFiveD && kind == PetsCellKind.SwitchToTwoPointFiveD)
                || (targetMode == PetsPerspectiveMode.TwoD && kind == PetsCellKind.SwitchTo2D);
        }

        public bool TryGetSwitchTile(PetsGridCoord coord, out PetsSwitchTile tile)
        {
            return switchTiles.TryGetValue(coord.ToVector2Int(), out tile);
        }

        public bool IsValidPlayerCell(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            if (Definition == null || !Definition.Contains(coord))
            {
                return false;
            }

            PetsCellKind kind = GetCell(coord, mode);
            if (mode == PetsPerspectiveMode.TwoD)
            {
                if (kind == PetsCellKind.BreakableBrick)
                {
                    return !IsBreakableBrickBlocking(coord);
                }

                return kind == PetsCellKind.WhiteInterior
                    || kind == PetsCellKind.WhiteLine
                    || kind == PetsCellKind.BlackRegion
                    || kind == PetsCellKind.SwitchTo2D
                    || kind == PetsCellKind.SwitchToTwoPointFiveD
                    || kind == PetsCellKind.Exit;
            }

            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit
                || kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox;
        }

        public bool IsBlackRegion(PetsGridCoord coord)
        {
            return GetCell(coord, currentMode) == PetsCellKind.BlackRegion;
        }

        public bool TryFindBlackRegionNear(Vector3 world, float searchRadius, out PetsGridCoord blackCoord)
        {
            int centerX = Mathf.RoundToInt(world.x / cellSize);
            int centerY = Mathf.RoundToInt(world.y / cellSize);
            int range = Mathf.Max(1, Mathf.CeilToInt(searchRadius / cellSize) + 1);

            for (int y = centerY - range; y <= centerY + range; y++)
            {
                for (int x = centerX - range; x <= centerX + range; x++)
                {
                    PetsGridCoord coord = new PetsGridCoord(x, y);
                    if (!IsBlackRegion(coord))
                    {
                        continue;
                    }

                    Vector3 center = GridToTwoDWorld(coord, 0f);
                    float halfSize = cellSize * 0.5f;
                    if (world.x >= center.x - halfSize - searchRadius
                        && world.x <= center.x + halfSize + searchRadius
                        && world.y >= center.y - halfSize - searchRadius
                        && world.y <= center.y + halfSize + searchRadius)
                    {
                        blackCoord = coord;
                        return true;
                    }
                }
            }

            blackCoord = default;
            return false;
        }

        public void CollectBlackRegionsNear(Bounds bounds, float padding, List<PetsGridCoord> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            float halfSize = cellSize * 0.5f;
            int minX = Mathf.FloorToInt((bounds.min.x - padding - halfSize) / cellSize);
            int maxX = Mathf.CeilToInt((bounds.max.x + padding + halfSize) / cellSize);
            int minY = Mathf.FloorToInt((bounds.min.y - padding - halfSize) / cellSize);
            int maxY = Mathf.CeilToInt((bounds.max.y + padding + halfSize) / cellSize);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    PetsGridCoord coord = new PetsGridCoord(x, y);
                    if (IsBlackRegion(coord))
                    {
                        results.Add(coord);
                    }
                }
            }
        }

        public bool IsExit(PetsGridCoord coord)
        {
            return GetCell(coord, currentMode) == PetsCellKind.Exit;
        }

        public bool IsBreakableBrick(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            return GetCell(coord, mode) == PetsCellKind.BreakableBrick;
        }

        public bool IsTopDownBlockedByProp(PetsGridCoord coord)
        {
            if (pushBoxes.ContainsKey(coord.ToVector2Int()))
            {
                return true;
            }

            if (IsBreakableBrickBlocking(coord))
            {
                return true;
            }

            return false;
        }

        private bool IsBreakableBrickBlocking(PetsGridCoord coord)
        {
            return breakableBricks.TryGetValue(coord.ToVector2Int(), out PetsBreakableBrick brick)
                && brick != null
                && !brick.IsBroken;
        }

        public bool TryPushBox(PetsGridCoord boxCoord, Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
            {
                return false;
            }

            Vector2Int boxKey = boxCoord.ToVector2Int();
            if (!pushBoxes.TryGetValue(boxKey, out PetsPushBox box) || box == null)
            {
                return false;
            }

            PetsGridCoord target = boxCoord + new PetsGridCoord(direction.x, direction.y);
            if (Definition == null || !Definition.Contains(target) || IsTopDownBlockedByProp(target))
            {
                return false;
            }

            if (!IsValidPlayerCell(target, PetsPerspectiveMode.TwoPointFiveD))
            {
                return false;
            }

            pushBoxes.Remove(boxKey);
            pushBoxes[target.ToVector2Int()] = box;
            box.MoveTo(target);
            return true;
        }

        public void NotifyBrickBroken(PetsGridCoord coord)
        {
            breakableBricks.Remove(coord.ToVector2Int());
        }

        public bool IsTopDownHole(PetsGridCoord coord)
        {
            return GetCell(coord, PetsPerspectiveMode.TwoPointFiveD) == PetsCellKind.BlackRegion || !IsValidPlayerCell(coord, PetsPerspectiveMode.TwoPointFiveD);
        }

        public bool IsTopDownJumpableHole(PetsGridCoord coord)
        {
            PetsCellKind kind = GetCell(coord, PetsPerspectiveMode.TwoPointFiveD);
            return kind == PetsCellKind.BlackRegion || kind == PetsCellKind.Empty;
        }

        public bool IsSolid2DLine(PetsGridCoord coord)
        {
            PetsCellKind kind = GetCell(coord, PetsPerspectiveMode.TwoD);
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit
                || kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox;
        }

        public bool CanUseTwoDVerticalWhiteStrip(PetsGridCoord coord, int verticalDirection)
        {
            if (IsTwoDVerticalWhiteStripCell(coord))
            {
                return true;
            }

            if (!IsTwoDClimbableWhiteCell(coord))
            {
                return false;
            }

            if (verticalDirection > 0)
            {
                return IsTwoDVerticalWhiteStripCell(coord + new PetsGridCoord(0, 1));
            }

            if (verticalDirection < 0)
            {
                return IsTwoDVerticalWhiteStripCell(coord + new PetsGridCoord(0, -1));
            }

            return false;
        }

        private bool IsTwoDVerticalWhiteStripCell(PetsGridCoord coord)
        {
            if (!IsTwoDClimbableWhiteCell(coord))
            {
                return false;
            }

            bool up = IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(0, 1));
            bool down = IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(0, -1));
            bool right = IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(1, 0));
            bool left = IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(-1, 0));
            bool hasVerticalNeighbor = up || down;
            int whiteNeighborCount = (up ? 1 : 0) + (down ? 1 : 0) + (right ? 1 : 0) + (left ? 1 : 0);
            bool partOfFilledPatch = (up && right && IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(1, 1)))
                || (up && left && IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(-1, 1)))
                || (down && right && IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(1, -1)))
                || (down && left && IsTwoDClimbableWhiteCell(coord + new PetsGridCoord(-1, -1)));

            return hasVerticalNeighbor && whiteNeighborCount <= 2 && !partOfFilledPatch;
        }

        private bool IsTwoDClimbableWhiteCell(PetsGridCoord coord)
        {
            if (Definition == null || !Definition.Contains(coord))
            {
                return false;
            }

            PetsCellKind kind = GetCell(coord, PetsPerspectiveMode.TwoD);
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit;
        }

        public override void SetPerspectiveMode(PetsPerspectiveMode mode)
        {
            currentMode = mode;
            SetObjects(twoDObjects, currentMode == PetsPerspectiveMode.TwoD);
            SetObjects(topDownObjects, currentMode == PetsPerspectiveMode.TwoPointFiveD);
            PositionSharedObjects();
        }

        private Transform CreateRoot(string rootName)
        {
            GameObject rootObject = new GameObject(rootName);
            rootObject.transform.SetParent(transform);
            return rootObject.transform;
        }

        private void BuildTwoDCell(Vector2Int coord, PetsCellKind kind, Material blackMaterial, Material brickMaterial)
        {
            if (kind == PetsCellKind.BlackRegion)
            {
                GameObject black2D = CreateBox(
                    $"2D Black Region {coord.x},{coord.y}",
                    twoDRoot,
                    new Vector3(coord.x * cellSize, coord.y * cellSize, 0.02f),
                    new Vector3(cellSize, cellSize, twoDLineDepth),
                    blackMaterial,
                    false,
                    false);
                black2D.AddComponent<PetsBlackRegion2D>().Configure(this, PetsGridCoord.FromVector2Int(coord));
                twoDObjects.Add(black2D);
            }

            if (kind == PetsCellKind.BreakableBrick)
            {
                BuildBreakableBrick(coord, brickMaterial);
            }
        }

        private void BuildMergedTwoDShapeInk(Material material)
        {
            Dictionary<int, List<Vector2Int>> northEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> southEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> eastEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> westEdges = new Dictionary<int, List<Vector2Int>>();

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoDCells)
            {
                if (!IsTwoDClosedShapeKind(cell.Value))
                {
                    continue;
                }

                if (cell.Value == PetsCellKind.BlackRegion)
                {
                    continue;
                }

                Vector2Int coord = cell.Key;
                if (!IsTwoDClosedShapeCell(coord + Vector2Int.up))
                {
                    AddBoundaryInterval(northEdges, coord.y + 1, coord.x, coord.x + 1);
                }

                if (!IsTwoDClosedShapeCell(coord + Vector2Int.down))
                {
                    AddBoundaryInterval(southEdges, coord.y, coord.x, coord.x + 1);
                }

                if (!IsTwoDClosedShapeCell(coord + Vector2Int.right))
                {
                    AddBoundaryInterval(eastEdges, coord.x + 1, coord.y, coord.y + 1);
                }

                if (!IsTwoDClosedShapeCell(coord + Vector2Int.left))
                {
                    AddBoundaryInterval(westEdges, coord.x, coord.y, coord.y + 1);
                }
            }

            DrawMergedTwoDShapeHorizontalEdges(northEdges, "North", material, 59);
            DrawMergedTwoDShapeHorizontalEdges(southEdges, "South", material, 61);
            DrawMergedTwoDShapeVerticalEdges(eastEdges, "East", material, 67);
            DrawMergedTwoDShapeVerticalEdges(westEdges, "West", material, 71);
        }

        private void BuildMergedTopDownShapeInk(Material material)
        {
            Dictionary<int, List<Vector2Int>> northEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> southEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> eastEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> westEdges = new Dictionary<int, List<Vector2Int>>();

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoPointFiveDCells)
            {
                if (!IsTopDownSurfaceKind(cell.Value))
                {
                    continue;
                }

                Vector2Int coord = cell.Key;
                if (!IsTopDownSurfaceCell(coord + Vector2Int.up))
                {
                    AddBoundaryInterval(northEdges, coord.y + 1, coord.x, coord.x + 1);
                }

                if (!IsTopDownSurfaceCell(coord + Vector2Int.down))
                {
                    AddBoundaryInterval(southEdges, coord.y, coord.x, coord.x + 1);
                }

                if (!IsTopDownSurfaceCell(coord + Vector2Int.right))
                {
                    AddBoundaryInterval(eastEdges, coord.x + 1, coord.y, coord.y + 1);
                }

                if (!IsTopDownSurfaceCell(coord + Vector2Int.left))
                {
                    AddBoundaryInterval(westEdges, coord.x, coord.y, coord.y + 1);
                }
            }

            DrawMergedTopDownShapeHorizontalEdges(northEdges, "North", material, 73);
            DrawMergedTopDownShapeHorizontalEdges(southEdges, "South", material, 79);
            DrawMergedTopDownShapeVerticalEdges(eastEdges, "East", material, 83);
            DrawMergedTopDownShapeVerticalEdges(westEdges, "West", material, 89);
        }

        private void BuildTwoPointFiveDCell(Vector2Int coord, PetsCellKind kind, Material whiteMaterial, Material blackMaterial, Material brickMaterial, Material boxMaterial)
        {
            if (IsTopDownSurfaceKind(kind))
            {
                GameObject platform = CreateBox(
                    $"2.5D Platform {coord.x},{coord.y}",
                    topDownRoot,
                    new Vector3(coord.x * cellSize, -topDownPlatformThickness * 0.5f, coord.y * cellSize),
                    new Vector3(cellSize, topDownPlatformThickness, cellSize),
                    whiteMaterial,
                    true,
                    false);
                topDownPlatforms[coord] = platform;
                topDownObjects.Add(platform);
            }

            if (kind == PetsCellKind.BlackRegion)
            {
                GameObject hole = CreateBox(
                    $"2.5D Hole {coord.x},{coord.y}",
                    topDownRoot,
                    new Vector3(coord.x * cellSize, -topDownHoleDepth, coord.y * cellSize),
                    new Vector3(cellSize * 0.9f, topDownHoleDepth, cellSize * 0.9f),
                    blackMaterial,
                    false,
                    false);
                topDownObjects.Add(hole);
            }

            if (kind == PetsCellKind.BreakableBrick && !breakableBricks.ContainsKey(coord))
            {
                BuildBreakableBrick(coord, brickMaterial);
            }

            if (kind == PetsCellKind.PushBox)
            {
                BuildPushBox(coord, boxMaterial);
            }
        }

        private void BuildBreakableBrick(Vector2Int coord, Material material)
        {
            GameObject root = new GameObject($"Breakable Brick {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            GameObject twoDView = CreateBox(
                "2D Brick",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize, -0.36f),
                new Vector3(cellSize * 0.88f, cellSize * 0.88f, 0.18f),
                material,
                true,
                true);

            GameObject topDownView = CreateBox(
                "2.5D Brick",
                root.transform,
                new Vector3(coord.x * cellSize, 0.38f, coord.y * cellSize),
                new Vector3(cellSize * 0.82f, 0.76f, cellSize * 0.82f),
                material,
                true,
                true);

            PetsBreakableBrick brick = root.AddComponent<PetsBreakableBrick>();
            brick.Configure(this, PetsGridCoord.FromVector2Int(coord), twoDView, topDownView);
            breakableBricks[coord] = brick;
        }

        private void BuildPushBox(Vector2Int coord, Material material)
        {
            GameObject root = new GameObject($"Push Box {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            GameObject twoDView = CreateBox(
                "2D Box",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize, -0.34f),
                new Vector3(cellSize * 0.78f, cellSize * 0.78f, 0.2f),
                material,
                true,
                true);
            AlignTwoDPropColliderToPhysicsPlane(twoDView);

            GameObject topDownView = CreateBox(
                "2.5D Box",
                root.transform,
                new Vector3(coord.x * cellSize, 0.42f, coord.y * cellSize),
                new Vector3(cellSize * 0.78f, 0.84f, cellSize * 0.78f),
                material,
                true,
                true);
            Rigidbody body = topDownView.AddComponent<Rigidbody>();
            body.mass = 2.4f;
            body.drag = 7f;
            body.angularDrag = 6f;
            PetsPushBox pushBox = root.AddComponent<PetsPushBox>();
            pushBox.Configure(this, PetsGridCoord.FromVector2Int(coord), twoDView, topDownView, body);
            pushBoxes[coord] = pushBox;
        }

        private void AlignTwoDPropColliderToPhysicsPlane(GameObject prop)
        {
            if (prop == null)
            {
                return;
            }

            BoxCollider collider = prop.GetComponent<BoxCollider>();
            if (collider == null)
            {
                return;
            }

            Vector3 localPhysicsCenter = prop.transform.InverseTransformPoint(new Vector3(prop.transform.position.x, prop.transform.position.y, 0f));
            Vector3 center = collider.center;
            center.z = localPhysicsCenter.z;
            collider.center = center;

            Vector3 size = collider.size;
            size.z = twoDLineDepth / Mathf.Max(0.001f, Mathf.Abs(prop.transform.lossyScale.z));
            collider.size = size;
        }

        private void BuildBlackRegionFills(Dictionary<Vector2Int, PetsCellKind> source, PetsPerspectiveMode mode, Material material)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            List<int> ys = new List<int>();
            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in source)
            {
                if (cell.Value == PetsCellKind.BlackRegion && !ys.Contains(cell.Key.y))
                {
                    ys.Add(cell.Key.y);
                }
            }

            ys.Sort();
            for (int i = 0; i < ys.Count; i++)
            {
                int y = ys[i];
                List<int> xs = new List<int>();
                foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in source)
                {
                    if (cell.Value == PetsCellKind.BlackRegion && cell.Key.y == y)
                    {
                        xs.Add(cell.Key.x);
                    }
                }

                xs.Sort();
                int index = 0;
                while (index < xs.Count)
                {
                    int startX = xs[index];
                    int endX = startX;
                    index++;
                    while (index < xs.Count && xs[index] == endX + 1)
                    {
                        endX = xs[index];
                        index++;
                    }

                    Vector2Int startCoord = new Vector2Int(startX, y);
                    if (visited.Contains(startCoord))
                    {
                        continue;
                    }

                    for (int x = startX; x <= endX; x++)
                    {
                        visited.Add(new Vector2Int(x, y));
                    }

                    float width = (endX - startX + 1) * cellSize;
                    float centerX = (startX + endX) * 0.5f * cellSize;
                    if (mode == PetsPerspectiveMode.TwoD)
                    {
                        GameObject fill = CreateBox(
                            $"2D Black Region Fill {startX}-{endX},{y}",
                            twoDRoot,
                            new Vector3(centerX, y * cellSize, -0.43f),
                            new Vector3(width, cellSize, 0.035f),
                            material,
                            false,
                            true);
                        twoDObjects.Add(fill);
                    }
                    else
                    {
                        GameObject fill = CreateBox(
                            $"2.5D Black Region Fill {startX}-{endX},{y}",
                            topDownRoot,
                            new Vector3(centerX, 0.095f, y * cellSize),
                            new Vector3(width, 0.035f, cellSize),
                            material,
                            false,
                            true);
                        topDownObjects.Add(fill);
                    }
                }
            }
        }

        private GameObject CreateMarker(Vector2Int coord, PetsCellKind kind, Material switchMaterial, Material exitMaterial)
        {
            GameObject marker = new GameObject($"{kind} Marker {coord.x},{coord.y}");
            marker.transform.SetParent(sharedRoot);
            marker.transform.position = new Vector3(coord.x * cellSize, coord.y * cellSize + 0.08f, -0.35f);
            marker.name = $"{kind} Marker {coord.x},{coord.y}";

            BoxCollider trigger = marker.AddComponent<BoxCollider>();
            trigger.size = new Vector3(cellSize * 0.86f, 0.32f, cellSize * 0.86f);
            trigger.isTrigger = true;

            GameObject plate = CreateBox(
                "Plate",
                marker.transform,
                marker.transform.position,
                new Vector3(cellSize * 0.78f, 0.08f, cellSize * 0.78f),
                kind == PetsCellKind.Exit ? exitMaterial : switchMaterial,
                false);
            plate.transform.localPosition = Vector3.zero;

            PetsSwitchTile switchTile = marker.AddComponent<PetsSwitchTile>();
            if (kind == PetsCellKind.SwitchTo2D || kind == PetsCellKind.SwitchToTwoPointFiveD)
            {
                PetsPerspectiveMode target = kind == PetsCellKind.SwitchTo2D ? PetsPerspectiveMode.TwoD : PetsPerspectiveMode.TwoPointFiveD;
                switchTile.Configure(this, PetsGridCoord.FromVector2Int(coord), target);
                switchTiles[coord] = switchTile;
            }
            else
            {
                switchTile.ConfigureAsExit(this, PetsGridCoord.FromVector2Int(coord));
            }

            TextMesh label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(marker.transform);
            label.transform.localPosition = new Vector3(0f, 0.09f, -0.05f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label.text = kind == PetsCellKind.SwitchTo2D ? "2D" : kind == PetsCellKind.SwitchToTwoPointFiveD ? "2.5D" : "EXIT";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = kind == PetsCellKind.SwitchToTwoPointFiveD ? 0.1f : kind == PetsCellKind.Exit ? 0.14f : 0.18f;
            label.fontSize = 48;
            label.color = Color.black;

            return marker;
        }

        private static bool IsMarkerKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit;
        }

        private static bool IsMarkerActive(PetsSwitchTile tile, PetsCellKind currentKind, PetsPerspectiveMode currentMode)
        {
            if (tile.IsExit)
            {
                return currentKind == PetsCellKind.Exit;
            }

            if (tile.TargetMode == currentMode)
            {
                return false;
            }

            return (tile.TargetMode == PetsPerspectiveMode.TwoD && currentKind == PetsCellKind.SwitchTo2D)
                || (tile.TargetMode == PetsPerspectiveMode.TwoPointFiveD && currentKind == PetsCellKind.SwitchToTwoPointFiveD);
        }

        private void PositionSharedObjects()
        {
            foreach (GameObject sharedObject in sharedObjects)
            {
                if (sharedObject == null)
                {
                    continue;
                }

                PetsSwitchTile tile = sharedObject.GetComponent<PetsSwitchTile>();
                if (tile == null)
                {
                    continue;
                }

                bool isActiveInCurrentMap = IsMarkerActive(tile, GetCell(tile.Coord, currentMode), currentMode);
                SetMarkerVisible(sharedObject, isActiveInCurrentMap);
                if (!isActiveInCurrentMap)
                {
                    continue;
                }

                Vector2Int coord = tile.Coord.ToVector2Int();
                if (currentMode == PetsPerspectiveMode.TwoD)
                {
                    sharedObject.transform.position = new Vector3(coord.x * cellSize, coord.y * cellSize + 0.16f, -0.35f);
                    sharedObject.transform.rotation = Quaternion.identity;
                }
                else
                {
                    sharedObject.transform.position = new Vector3(coord.x * cellSize, 0.08f, coord.y * cellSize);
                    sharedObject.transform.rotation = Quaternion.identity;
                }

                TextMesh label = sharedObject.GetComponentInChildren<TextMesh>();
                if (label != null)
                {
                    label.transform.localPosition = currentMode == PetsPerspectiveMode.TwoD
                        ? new Vector3(0f, 0.1f, -0.05f)
                        : new Vector3(0f, 0.16f, -0.09f);
                    label.transform.localRotation = currentMode == PetsPerspectiveMode.TwoD
                        ? Quaternion.identity
                        : Quaternion.Euler(60f, 0f, 0f);
                }
            }
        }

        private static void SetMarkerVisible(GameObject marker, bool visible)
        {
            Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = visible;
            }

            Collider[] colliders = marker.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = visible;
            }
        }

        private void BuildMergedBlackRegionInk(Dictionary<Vector2Int, PetsCellKind> source, PetsPerspectiveMode mode, Material material)
        {
            Dictionary<int, List<Vector2Int>> northEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> southEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> eastEdges = new Dictionary<int, List<Vector2Int>>();
            Dictionary<int, List<Vector2Int>> westEdges = new Dictionary<int, List<Vector2Int>>();

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in source)
            {
                if (cell.Value != PetsCellKind.BlackRegion)
                {
                    continue;
                }

                Vector2Int coord = cell.Key;
                if (!IsBlackRegionCell(source, coord + Vector2Int.up))
                {
                    AddBoundaryInterval(northEdges, coord.y + 1, coord.x, coord.x + 1);
                }

                if (!IsBlackRegionCell(source, coord + Vector2Int.down))
                {
                    AddBoundaryInterval(southEdges, coord.y, coord.x, coord.x + 1);
                }

                if (!IsBlackRegionCell(source, coord + Vector2Int.right))
                {
                    AddBoundaryInterval(eastEdges, coord.x + 1, coord.y, coord.y + 1);
                }

                if (!IsBlackRegionCell(source, coord + Vector2Int.left))
                {
                    AddBoundaryInterval(westEdges, coord.x, coord.y, coord.y + 1);
                }
            }

            DrawMergedHorizontalEdges(northEdges, mode, "North", material, 11);
            DrawMergedHorizontalEdges(southEdges, mode, "South", material, 23);
            DrawMergedVerticalEdges(eastEdges, mode, "East", material, 37);
            DrawMergedVerticalEdges(westEdges, mode, "West", material, 43);
        }

        private void AddTwoDStroke(string name, Vector3 start, Vector3 end, Material material, int seed)
        {
            GameObject stroke = PetsInkStrokeBuilder.CreateStroke(name, twoDRoot, start, end, Vector3.forward, material, inkLineWidth2D, inkWobble, seed);
            twoDObjects.Add(stroke);
        }

        private void DrawMergedTwoDShapeHorizontalEdges(Dictionary<int, List<Vector2Int>> groups, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeY = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeY]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x0 = EdgeToWorld(interval.x);
                    float x1 = EdgeToWorld(interval.y);
                    float y = EdgeToWorld(edgeY);
                    GameObject edgeCollider = CreateBox(
                        $"2D Closed Shape Collider {sideName} {interval.x},{edgeY}",
                        twoDRoot,
                        new Vector3((x0 + x1) * 0.5f, y, 0f),
                        new Vector3(Mathf.Abs(x1 - x0), twoDLineThickness, twoDLineDepth),
                        material,
                        true,
                        false);
                    twoDObjects.Add(edgeCollider);
                    int seed = edgeY * 283 + interval.x * 409 + seedOffset;
                    AddTwoDStroke($"2D Closed Shape Outer {sideName} {interval.x},{edgeY}", new Vector3(x0, y, -0.42f), new Vector3(x1, y, -0.42f), material, seed);
                }
            }
        }

        private void DrawMergedTwoDShapeVerticalEdges(Dictionary<int, List<Vector2Int>> groups, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeX = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeX]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x = EdgeToWorld(edgeX);
                    float y0 = EdgeToWorld(interval.x);
                    float y1 = EdgeToWorld(interval.y);
                    GameObject edgeCollider = CreateBox(
                        $"2D Closed Shape Collider {sideName} {edgeX},{interval.x}",
                        twoDRoot,
                        new Vector3(x, (y0 + y1) * 0.5f, 0f),
                        new Vector3(twoDLineThickness, Mathf.Abs(y1 - y0), twoDLineDepth),
                        material,
                        true,
                        false);
                    twoDObjects.Add(edgeCollider);
                    int seed = edgeX * 293 + interval.x * 419 + seedOffset;
                    AddTwoDStroke($"2D Closed Shape Outer {sideName} {edgeX},{interval.x}", new Vector3(x, y0, -0.42f), new Vector3(x, y1, -0.42f), material, seed);
                }
            }
        }

        private void DrawMergedTopDownShapeHorizontalEdges(Dictionary<int, List<Vector2Int>> groups, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeY = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeY]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x0 = EdgeToWorld(interval.x);
                    float x1 = EdgeToWorld(interval.y);
                    float z = EdgeToWorld(edgeY);
                    int seed = edgeY * 307 + interval.x * 431 + seedOffset;
                    AddTopDownFlattenedStroke($"2.5D Flattened Shape Outer {sideName} {interval.x},{edgeY}", new Vector3(x0, 0.09f, z), new Vector3(x1, 0.09f, z), material, seed);
                }
            }
        }

        private void DrawMergedTopDownShapeVerticalEdges(Dictionary<int, List<Vector2Int>> groups, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeX = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeX]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x = EdgeToWorld(edgeX);
                    float z0 = EdgeToWorld(interval.x);
                    float z1 = EdgeToWorld(interval.y);
                    int seed = edgeX * 311 + interval.x * 439 + seedOffset;
                    AddTopDownFlattenedStroke($"2.5D Flattened Shape Outer {sideName} {edgeX},{interval.x}", new Vector3(x, 0.09f, z0), new Vector3(x, 0.09f, z1), material, seed);
                }
            }
        }

        private static void AddBoundaryInterval(Dictionary<int, List<Vector2Int>> groups, int fixedAxis, int start, int end)
        {
            if (!groups.TryGetValue(fixedAxis, out List<Vector2Int> intervals))
            {
                intervals = new List<Vector2Int>();
                groups[fixedAxis] = intervals;
            }

            intervals.Add(new Vector2Int(start, end));
        }

        private void DrawMergedHorizontalEdges(Dictionary<int, List<Vector2Int>> groups, PetsPerspectiveMode mode, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeY = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeY]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x0 = EdgeToWorld(interval.x);
                    float x1 = EdgeToWorld(interval.y);
                    float y = EdgeToWorld(edgeY);
                    int seed = edgeY * 271 + interval.x * 397 + seedOffset;
                    if (mode == PetsPerspectiveMode.TwoD)
                    {
                        AddTwoDStroke($"2D Black Region Outer {sideName} {interval.x},{edgeY}", new Vector3(x0, y, -0.44f), new Vector3(x1, y, -0.44f), material, seed);
                    }
                    else
                    {
                        AddTopDownStroke($"2.5D Hole Outer {sideName} {interval.x},{edgeY}", new Vector3(x0, 0.09f, y), new Vector3(x1, 0.09f, y), material, seed + 1000);
                    }
                }
            }
        }

        private void DrawMergedVerticalEdges(Dictionary<int, List<Vector2Int>> groups, PetsPerspectiveMode mode, string sideName, Material material, int seedOffset)
        {
            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int edgeX = keys[i];
                List<Vector2Int> merged = MergeIntervals(groups[edgeX]);
                for (int j = 0; j < merged.Count; j++)
                {
                    Vector2Int interval = merged[j];
                    float x = EdgeToWorld(edgeX);
                    float y0 = EdgeToWorld(interval.x);
                    float y1 = EdgeToWorld(interval.y);
                    int seed = edgeX * 277 + interval.x * 401 + seedOffset;
                    if (mode == PetsPerspectiveMode.TwoD)
                    {
                        AddTwoDStroke($"2D Black Region Outer {sideName} {edgeX},{interval.x}", new Vector3(x, y0, -0.44f), new Vector3(x, y1, -0.44f), material, seed);
                    }
                    else
                    {
                        AddTopDownStroke($"2.5D Hole Outer {sideName} {edgeX},{interval.x}", new Vector3(x, 0.09f, y0), new Vector3(x, 0.09f, y1), material, seed + 1000);
                    }
                }
            }
        }

        private static List<Vector2Int> MergeIntervals(List<Vector2Int> intervals)
        {
            intervals.Sort((left, right) => left.x != right.x ? left.x.CompareTo(right.x) : left.y.CompareTo(right.y));
            List<Vector2Int> merged = new List<Vector2Int>();
            if (intervals.Count == 0)
            {
                return merged;
            }

            int start = intervals[0].x;
            int end = intervals[0].y;
            for (int i = 1; i < intervals.Count; i++)
            {
                Vector2Int interval = intervals[i];
                if (interval.x <= end)
                {
                    end = Mathf.Max(end, interval.y);
                    continue;
                }

                merged.Add(new Vector2Int(start, end));
                start = interval.x;
                end = interval.y;
            }

            merged.Add(new Vector2Int(start, end));
            return merged;
        }

        private float EdgeToWorld(int edge)
        {
            return (edge - 0.5f) * cellSize;
        }

        private void AddTopDownStroke(string name, Vector3 start, Vector3 end, Material material, int seed)
        {
            GameObject stroke = PetsInkStrokeBuilder.CreateStroke(name, topDownRoot, start, end, Vector3.up, material, inkLineWidth25D, inkWobble, seed);
            topDownObjects.Add(stroke);
        }

        private void AddTopDownFlattenedStroke(string name, Vector3 start, Vector3 end, Material material, int seed)
        {
            GameObject shadow = PetsInkStrokeBuilder.CreateStroke(
                $"{name} Ground Perspective Shadow",
                topDownRoot,
                start + flattenedShadowOffset,
                end + flattenedShadowOffset,
                Vector3.up,
                GetFlattenedShadowMaterial(material),
                inkLineWidth25D * 0.75f,
                inkWobble * 0.55f,
                seed + 5000,
                9,
                Color.white);
            topDownObjects.Add(shadow);

            AddTopDownStroke(name, start, end, material, seed);
        }

        private Material GetFlattenedShadowMaterial(Material source)
        {
            if (flattenedShadowMaterial != null)
            {
                return flattenedShadowMaterial;
            }

            flattenedShadowMaterial = new Material(source)
            {
                name = "Flattened Ground Perspective Cue"
            };
            if (flattenedShadowMaterial.HasProperty("_Color"))
            {
                flattenedShadowMaterial.color = flattenedShadowColor;
            }

            return flattenedShadowMaterial;
        }

        private bool IsTopDownSurfaceCell(Vector2Int coord)
        {
            return twoPointFiveDCells.TryGetValue(coord, out PetsCellKind kind) && IsTopDownSurfaceKind(kind);
        }

        private bool IsTwoDSurfaceCell(Vector2Int coord)
        {
            return twoDCells.TryGetValue(coord, out PetsCellKind kind) && IsTwoDSurfaceKind(kind);
        }

        private bool IsTwoDClosedShapeCell(Vector2Int coord)
        {
            return twoDCells.TryGetValue(coord, out PetsCellKind kind) && IsTwoDClosedShapeKind(kind);
        }

        private static bool IsBlackRegionCell(Dictionary<Vector2Int, PetsCellKind> source, Vector2Int coord)
        {
            return source.TryGetValue(coord, out PetsCellKind kind) && kind == PetsCellKind.BlackRegion;
        }

        private static bool IsTwoDSurfaceKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BlackRegion
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit
                || kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox;
        }

        private static bool IsTwoDClosedShapeKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BlackRegion
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit
                || kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox;
        }

        private static bool IsTopDownSurfaceKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit
                || kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid, bool visible = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = visible;
            Collider collider = box.GetComponent<Collider>();
            collider.enabled = solid;
            return box;
        }

        private static void SetObjects(List<GameObject> objects, bool active)
        {
            foreach (GameObject item in objects)
            {
                if (item != null)
                {
                    item.SetActive(active);
                }
            }
        }
    }
}
