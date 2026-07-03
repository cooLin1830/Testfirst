using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DimensionShift.PetsLike
{
    public sealed class PetsLevelRuntime : PetsPerspectiveListenerBehaviour
    {
        private const string StarFbxPath = "Assets/Art/3D/star.fbx";
        private const string Star2DVisualPrefabPath = "Assets/Art/2d/star/Star2DVisual.prefab";
        private const string Star2DFrame1Path = "Assets/Art/2d/star/star1.png";
        private const string Star2DFrame2Path = "Assets/Art/2d/star/satr2.png";
        private const string Star2DFrame3Path = "Assets/Art/2d/star/star3.png";
        private const string Portal2DVisualPrefabPath = "Assets/Art/2d/item/Portal2DVisual.prefab";
        private const string Portal2DFrame1Path = "Assets/Art/2d/item/portal1.png";
        private const string Portal2DFrame2Path = "Assets/Art/2d/item/portal2.png";
        private const string Portal2DFrame3Path = "Assets/Art/2d/item/portal3.png";
        private const string Portal2DFrame4Path = "Assets/Art/2d/item/portal4.png";
        private const string Switch2DIconPath = "Assets/Art/2d/item/2D.png";
        private const string Switch25DIconPath = "Assets/Art/2d/item/2.5d.png";
        private const string TwoDBrickSpritePath = "Assets/Art/3D/brick.png";
        private const string TwoDBoxSpritePath = "Assets/Art/3D/box.png";
        private const string TopDownBrickFbxPath = "Assets/Art/3D/brick.fbx";
        private const string TopDownBoxFbxPath = "Assets/Art/3D/boxfbx.fbx";
        private const float TwoDStarSpriteScale = 0.035f;
        private const float TwoDStarFallbackScale = 0.28f;
        private const float TwoDStarCoveredDepth = -0.28f;
        private const float TwoDStarVisibleDepth = -0.46f;
        private const float TopDownStarVisualDiameter = 0.68f;
        private const float TopDownStarGroundY = 0.1f;
        private const float Portal2DVisualScale = 0.045f;
        private const float Portal2DVerticalOffset = 0.16f;
        private const float Portal2DDepth = -0.36f;
        private const float SwitchIconWorldSize = 0.62f;

        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float twoDLineThickness = 0.18f;
        [SerializeField] private float twoDLineDepth = 0.8f;
        [SerializeField] private float topDownPlatformThickness = 0.16f;
        [SerializeField] private float topDownHoleDepth = 0.28f;
        [SerializeField] private float inkLineWidth2D = 0.055f;
        [SerializeField] private float inkLineWidth25D = 0.045f;
        [SerializeField] private float inkWobble = 0.025f;
        [SerializeField] private float bouncePadVelocity = 10.5f;
        [SerializeField] private Vector3 flattenedShadowOffset = new Vector3(0.08f, -0.045f, -0.12f);
        [SerializeField] private Color flattenedShadowColor = new Color(0.72f, 0.72f, 0.68f, 1f);
        [SerializeField] private Color starColor = new Color(1f, 0.86f, 0.18f);

        private readonly Dictionary<Vector2Int, PetsCellKind> twoDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsCellKind> twoPointFiveDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsPropKind> propCells = new Dictionary<Vector2Int, PetsPropKind>();
        private readonly HashSet<Vector2Int> starCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, PetsStarCollectible> starCollectibles = new Dictionary<Vector2Int, PetsStarCollectible>();
        private readonly Dictionary<Vector2Int, PetsCellKind> markerCells = new Dictionary<Vector2Int, PetsCellKind>();
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
        private Material starMaterial;
        private PetsPerspectiveMode currentMode;
        private int totalStars;
        private int collectedStars;

        public PetsLevelDefinition Definition { get; private set; }
        public float CellSize => cellSize;
        public int TotalStars => totalStars;
        public int CollectedStars => collectedStars;
        public bool HasCollectedAllStars => collectedStars >= totalStars;

        public void Build(PetsLevelDefinition definition, Material whiteMaterial, Material blackMaterial, Material switchMaterial, Material exitMaterial, Material brickMaterial, Material boxMaterial, Material bouncePadMaterial)
        {
            Definition = definition;
            cellSize = definition.CellSize;
            twoDCells.Clear();
            twoPointFiveDCells.Clear();
            propCells.Clear();
            starCells.Clear();
            starCollectibles.Clear();
            markerCells.Clear();
            switchTiles.Clear();
            topDownPlatforms.Clear();
            breakableBricks.Clear();
            pushBoxes.Clear();
            twoDObjects.Clear();
            topDownObjects.Clear();
            sharedObjects.Clear();
            totalStars = 0;
            collectedStars = 0;

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

            foreach (KeyValuePair<Vector2Int, PetsPropKind> prop in definition.Props)
            {
                if (prop.Value == PetsPropKind.Star)
                {
                    starCells.Add(prop.Key);
                }
                else
                {
                    propCells[prop.Key] = prop.Value;
                }
            }

            foreach (Vector2Int star in definition.Stars)
            {
                starCells.Add(star);
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> marker in definition.Markers)
            {
                markerCells[marker.Key] = marker.Value;
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoDCells)
            {
                BuildTwoDCell(cell.Key, cell.Value, blackMaterial, bouncePadMaterial);
            }

            foreach (KeyValuePair<Vector2Int, PetsCellKind> cell in twoPointFiveDCells)
            {
                BuildTwoPointFiveDCell(cell.Key, cell.Value, whiteMaterial, blackMaterial);
            }

            foreach (KeyValuePair<Vector2Int, PetsPropKind> prop in propCells)
            {
                BuildProp(prop.Key, prop.Value, brickMaterial, boxMaterial);
            }

            foreach (Vector2Int star in starCells)
            {
                BuildStar(star);
            }

            BuildSpawnPortal(definition.Spawn.ToVector2Int());

            BuildBlackRegionFills(twoDCells, PetsPerspectiveMode.TwoD, blackMaterial);
            BuildBlackRegionFills(twoPointFiveDCells, PetsPerspectiveMode.TwoPointFiveD, blackMaterial);
            BuildMergedTwoDShapeInk(blackMaterial);
            BuildMergedTopDownShapeInk(blackMaterial);

            foreach (KeyValuePair<Vector2Int, PetsCellKind> markerCell in markerCells)
            {
                if (IsMarkerKind(markerCell.Value))
                {
                    GameObject marker = CreateMarker(markerCell.Key, markerCell.Value, switchMaterial, exitMaterial);
                    marker.transform.SetParent(sharedRoot);
                    sharedObjects.Add(marker);
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
            PetsCellKind kind = GetMarker(coord);
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
                return kind == PetsCellKind.WhiteInterior
                    || kind == PetsCellKind.WhiteLine
                    || kind == PetsCellKind.BlackRegion
                    || kind == PetsCellKind.BouncePad;
            }

            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BouncePad;
        }

        public PetsCellKind GetMarker(PetsGridCoord coord)
        {
            return markerCells.TryGetValue(coord.ToVector2Int(), out PetsCellKind kind) ? kind : PetsCellKind.Empty;
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
            return GetMarker(coord) == PetsCellKind.Exit;
        }

        public bool CanReachExit(PetsGridCoord coord)
        {
            return IsExit(coord) && HasCollectedAllStars;
        }

        public void NotifyStarCollected(PetsGridCoord coord)
        {
            if (!starCells.Contains(coord.ToVector2Int()))
            {
                return;
            }

            collectedStars = Mathf.Clamp(collectedStars + 1, 0, totalStars);
        }

        public bool IsBreakableBrick(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            return IsBreakableBrickBlocking(coord);
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

        private bool IsStarCoveredInTwoD(Vector2Int coord)
        {
            return pushBoxes.ContainsKey(coord)
                || IsBreakableBrickBlocking(PetsGridCoord.FromVector2Int(coord));
        }

        private void RefreshStarCover(Vector2Int coord)
        {
            if (starCollectibles.TryGetValue(coord, out PetsStarCollectible star) && star != null)
            {
                star.SetCoveredInTwoD(IsStarCoveredInTwoD(coord));
            }
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
            Vector2Int targetKey = target.ToVector2Int();
            pushBoxes[targetKey] = box;
            box.MoveTo(target);
            RefreshStarCover(boxKey);
            RefreshStarCover(targetKey);
            return true;
        }

        public void NotifyBrickBroken(PetsGridCoord coord)
        {
            Vector2Int key = coord.ToVector2Int();
            breakableBricks.Remove(key);
            RefreshStarCover(key);
        }

        public bool TryBreakFootLandingBrickNear(Bounds bounds)
        {
            if (breakableBricks.Count == 0)
            {
                return false;
            }

            PetsBreakableBrick target = null;
            float footY = bounds.min.y;
            foreach (KeyValuePair<Vector2Int, PetsBreakableBrick> entry in breakableBricks)
            {
                PetsBreakableBrick brick = entry.Value;
                if (brick == null || brick.IsBroken || !brick.CanBreakFromFootLanding)
                {
                    continue;
                }

                Vector3 center = GridToTwoDWorld(PetsGridCoord.FromVector2Int(entry.Key), 0f);
                float halfSize = cellSize * 0.5f;
                bool overlapsX = bounds.max.x > center.x - halfSize + 0.06f && bounds.min.x < center.x + halfSize - 0.06f;
                bool footNearTop = footY >= center.y + halfSize - 0.18f && footY <= center.y + halfSize + 0.24f;
                if (!overlapsX || !footNearTop)
                {
                    continue;
                }

                target = brick;
                break;
            }

            if (target == null)
            {
                return false;
            }

            return target.RegisterFootLanding();
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
                || kind == PetsCellKind.BouncePad;
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
                || kind == PetsCellKind.BouncePad;
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

        private void BuildTwoDCell(Vector2Int coord, PetsCellKind kind, Material blackMaterial, Material bouncePadMaterial)
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

            if (kind == PetsCellKind.BouncePad)
            {
                BuildBouncePad(coord, bouncePadMaterial);
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

        private void BuildTwoPointFiveDCell(Vector2Int coord, PetsCellKind kind, Material whiteMaterial, Material blackMaterial)
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

        }

        private void BuildProp(Vector2Int coord, PetsPropKind kind, Material brickMaterial, Material boxMaterial)
        {
            switch (kind)
            {
                case PetsPropKind.BreakableBrick:
                    BuildBreakableBrick(coord, brickMaterial, PetsBreakablePropRule.FootLanding);
                    break;
                case PetsPropKind.HeadBreakBox:
                    BuildBreakableBrick(coord, boxMaterial, PetsBreakablePropRule.TwoDHeadHit);
                    break;
                case PetsPropKind.PushBox:
                    BuildPushBox(coord, boxMaterial);
                    break;
                case PetsPropKind.Star:
                    break;
            }
        }

        private void BuildBreakableBrick(Vector2Int coord, Material material, PetsBreakablePropRule breakRule)
        {
            string displayName = breakRule == PetsBreakablePropRule.TwoDHeadHit ? "Head Break Box" : "Breakable Brick";
            string twoDSpritePath = breakRule == PetsBreakablePropRule.TwoDHeadHit ? TwoDBoxSpritePath : TwoDBrickSpritePath;
            GameObject root = new GameObject($"{displayName} {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            GameObject twoDView = CreateTwoDPropSpriteView(
                $"2D {displayName}",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize, -0.36f),
                new Vector2(cellSize * 0.88f, cellSize * 0.88f),
                twoDSpritePath,
                material);
            AlignTwoDPropColliderToPhysicsPlane(twoDView);

            string topDownModelPath = breakRule == PetsBreakablePropRule.TwoDHeadHit ? TopDownBoxFbxPath : TopDownBrickFbxPath;
            GameObject topDownView = CreateTopDownPropModelView(
                $"2.5D {displayName}",
                root.transform,
                new Vector3(coord.x * cellSize, 0.38f, coord.y * cellSize),
                new Vector3(cellSize * 0.82f, 0.76f, cellSize * 0.82f),
                topDownModelPath,
                material);

            PetsBreakableBrick brick = root.AddComponent<PetsBreakableBrick>();
            brick.Configure(this, PetsGridCoord.FromVector2Int(coord), twoDView, topDownView, breakRule);
            breakableBricks[coord] = brick;
        }

        private void BuildPushBox(Vector2Int coord, Material material)
        {
            GameObject root = new GameObject($"Push Box {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            GameObject twoDView = CreateTwoDPropSpriteView(
                "2D Box",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize, -0.34f),
                new Vector2(cellSize * 0.78f, cellSize * 0.78f),
                TwoDBoxSpritePath,
                material);
            AlignTwoDPropColliderToPhysicsPlane(twoDView);

            GameObject topDownView = CreateTopDownPropModelView(
                "2.5D Box",
                root.transform,
                new Vector3(coord.x * cellSize, 0.42f, coord.y * cellSize),
                new Vector3(cellSize * 0.78f, 0.84f, cellSize * 0.78f),
                TopDownBoxFbxPath,
                material);
            Rigidbody body = topDownView.AddComponent<Rigidbody>();
            body.mass = 2.4f;
            body.drag = 7f;
            body.angularDrag = 6f;
            PetsPushBox pushBox = root.AddComponent<PetsPushBox>();
            pushBox.Configure(this, PetsGridCoord.FromVector2Int(coord), twoDView, topDownView, body);
            pushBoxes[coord] = pushBox;
        }

        private void BuildBouncePad(Vector2Int coord, Material material)
        {
            GameObject root = new GameObject($"Bounce Pad {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            GameObject twoDView = CreateBox(
                "2D Bounce Pad",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize - cellSize * 0.32f, -0.32f),
                new Vector3(cellSize * 0.72f, cellSize * 0.16f, 0.18f),
                material,
                true,
                true);
            AlignTwoDPropColliderToPhysicsPlane(twoDView);

            GameObject triggerObject = CreateBox(
                "2D Bounce Trigger",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize - cellSize * 0.08f, 0f),
                new Vector3(cellSize * 0.82f, cellSize * 0.28f, twoDLineDepth),
                material,
                true,
                false);
            BoxCollider trigger = triggerObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;

            PetsBouncePad bouncePad = triggerObject.AddComponent<PetsBouncePad>();
            bouncePad.Configure(twoDView, trigger, bouncePadVelocity);
            twoDObjects.Add(root);
        }

        private void BuildStar(Vector2Int coord)
        {
            totalStars++;

            GameObject root = new GameObject($"Star Collectible {coord.x},{coord.y}");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            Material material = GetStarMaterial();
            float twoDStarZ = IsStarCoveredInTwoD(coord) ? TwoDStarCoveredDepth : TwoDStarVisibleDepth;
            GameObject twoDView = CreateTwoDStarView(
                "2D Star",
                root.transform,
                new Vector3(coord.x * cellSize, coord.y * cellSize + cellSize * 0.14f, twoDStarZ),
                Quaternion.identity,
                Vector3.one * (cellSize * TwoDStarSpriteScale),
                Vector3.one * (cellSize * TwoDStarFallbackScale),
                material);

            GameObject topDownView = CreateTopDownStarView(
                "2.5D Star",
                root.transform,
                new Vector3(coord.x * cellSize, TopDownStarGroundY, coord.y * cellSize),
                Quaternion.Euler(0f, 20f, 0f),
                Vector3.one * (cellSize * 0.42f),
                material);

            GameObject triggerObject = new GameObject("Star Trigger");
            triggerObject.transform.SetParent(root.transform);
            triggerObject.transform.position = new Vector3(coord.x * cellSize, coord.y * cellSize, 0f);
            BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;

            PetsStarCollectible star = triggerObject.AddComponent<PetsStarCollectible>();
            star.Configure(this, PetsGridCoord.FromVector2Int(coord), twoDView, topDownView, TwoDStarCoveredDepth, TwoDStarVisibleDepth);
            starCollectibles[coord] = star;
            RefreshStarCover(coord);
            sharedObjects.Add(root);
        }

        private void BuildSpawnPortal(Vector2Int coord)
        {
            GameObject root = new GameObject($"Spawn Portal {coord.x},{coord.y}");
            root.transform.SetParent(twoDRoot);
            root.transform.position = new Vector3(
                coord.x * cellSize,
                coord.y * cellSize + cellSize * Portal2DVerticalOffset,
                Portal2DDepth);

            if (!TryCreatePortalVisual(root.transform, "Portal", Vector3.zero, Quaternion.identity, 18))
            {
                CreateMarkerLabel(root.transform, "START", 0.12f);
            }

            twoDObjects.Add(root);
        }

        private GameObject CreateTwoDStarView(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 spriteScale, Vector3 fallbackScale, Material material)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Star2DVisualPrefabPath);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = name;
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.transform.localScale = spriteScale;
                RemoveGeneratedColliders(instance);
                ConfigureTwoDStarAnimation(instance);
                return instance;
            }
#endif

            return CreateFallbackStarView(name, parent, position, rotation, fallbackScale, material);
        }

        private GameObject CreateTopDownStarView(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarFbxPath);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = name;
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.transform.localScale = scale;
                RemoveGeneratedColliders(instance);
                ApplyMaterialToRenderers(instance, material);
                FitRenderedBounds(instance, cellSize * TopDownStarVisualDiameter);
                PlaceRenderedBoundsOnGround(instance, position);
                return instance;
            }
#endif

            return CreateFallbackStarView(name, parent, position, rotation, scale, material);
        }

#if UNITY_EDITOR
        private static void ConfigureTwoDStarAnimation(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (spriteRenderer == null)
            {
                return;
            }

            Sprite frame1 = AssetDatabase.LoadAssetAtPath<Sprite>(Star2DFrame1Path);
            Sprite frame2 = AssetDatabase.LoadAssetAtPath<Sprite>(Star2DFrame2Path);
            Sprite frame3 = AssetDatabase.LoadAssetAtPath<Sprite>(Star2DFrame3Path);
            if (frame1 == null || frame2 == null || frame3 == null)
            {
                return;
            }

            PetsSpriteFrameAnimator frameAnimator = instance.GetComponent<PetsSpriteFrameAnimator>();
            if (frameAnimator == null)
            {
                frameAnimator = instance.AddComponent<PetsSpriteFrameAnimator>();
            }

            frameAnimator.Configure(spriteRenderer, new[] { frame1, frame2, frame3 }, 6f);
        }
#endif

        private GameObject CreateFallbackStarView(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = name;
            fallback.transform.SetParent(parent);
            fallback.transform.position = position;
            fallback.transform.rotation = rotation;
            fallback.transform.localScale = scale;
            Renderer renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            RemoveGeneratedCollider(fallback.GetComponent<Collider>());
            return fallback;
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].gameObject.SetActive(true);
                    renderers[i].sharedMaterial = material;
                    renderers[i].enabled = true;
                }
            }
        }

        private static void FitRenderedBounds(GameObject root, float targetDiameter)
        {
            if (root == null || targetDiameter <= 0f || !TryGetRenderedBounds(root, out Bounds bounds))
            {
                return;
            }

            float currentDiameter = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (currentDiameter <= 0.0001f)
            {
                return;
            }

            root.transform.localScale *= targetDiameter / currentDiameter;
        }

        private static void PlaceRenderedBoundsOnGround(GameObject root, Vector3 groundPosition)
        {
            if (root == null || !TryGetRenderedBounds(root, out Bounds bounds))
            {
                return;
            }

            Vector3 anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            root.transform.position += groundPosition - anchor;
        }

        private static bool TryGetRenderedBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
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

        private Material GetStarMaterial()
        {
            if (starMaterial != null)
            {
                return starMaterial;
            }

            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            starMaterial = new Material(shader)
            {
                name = "Star Collectible"
            };
            starMaterial.color = starColor;
            if (shader.name == "Standard")
            {
                starMaterial.SetFloat("_Glossiness", 0.2f);
                starMaterial.EnableKeyword("_EMISSION");
                starMaterial.SetColor("_EmissionColor", starColor * 0.18f);
            }

            return starMaterial;
        }

        private static void RemoveGeneratedColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                RemoveGeneratedCollider(colliders[i]);
            }
        }

        private static void RemoveGeneratedCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
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

        private GameObject CreateTwoDPropSpriteView(string name, Transform parent, Vector3 position, Vector2 size, string spritePath, Material fallbackMaterial)
        {
#if UNITY_EDITOR
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                GameObject view = new GameObject(name);
                view.transform.SetParent(parent);
                view.transform.position = position;

                SpriteRenderer spriteRenderer = view.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = 20;

                Vector2 spriteWorldSize = sprite.bounds.size;
                view.transform.localScale = new Vector3(
                    size.x / Mathf.Max(0.001f, spriteWorldSize.x),
                    size.y / Mathf.Max(0.001f, spriteWorldSize.y),
                    1f);

                BoxCollider collider = view.AddComponent<BoxCollider>();
                collider.size = new Vector3(spriteWorldSize.x, spriteWorldSize.y, twoDLineDepth);
                return view;
            }
#endif

            return CreateBox(
                name,
                parent,
                position,
                new Vector3(size.x, size.y, 0.18f),
                fallbackMaterial,
                true,
                true);
        }

        private GameObject CreateTopDownPropModelView(string name, Transform parent, Vector3 position, Vector3 colliderScale, string modelPath, Material fallbackMaterial)
        {
            GameObject view = CreateBox(
                name,
                parent,
                position,
                colliderScale,
                fallbackMaterial,
                true,
                false);

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                GameObject model = Object.Instantiate(prefab, view.transform);
                model.name = $"{name} Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                RemoveGeneratedColliders(model);
                FitVisualToCollider(model.transform, colliderScale);
                return view;
            }
#endif

            Renderer renderer = view.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }

            return view;
        }

        private static void FitVisualToCollider(Transform visualRoot, Vector3 targetSize)
        {
            if (visualRoot == null)
            {
                return;
            }

            Bounds bounds = CalculateRendererBounds(visualRoot);
            Vector3 size = bounds.size;
            if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f)
            {
                return;
            }

            float scale = Mathf.Min(
                targetSize.x / size.x,
                targetSize.y / size.y,
                targetSize.z / size.z);
            visualRoot.localScale = Vector3.one * scale;

            Bounds scaledBounds = CalculateRendererBounds(visualRoot);
            Vector3 offset = visualRoot.position - scaledBounds.center;
            visualRoot.position += offset;
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
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

            if (kind == PetsCellKind.SwitchTo2D || kind == PetsCellKind.SwitchToTwoPointFiveD)
            {
                string iconPath = kind == PetsCellKind.SwitchTo2D ? Switch2DIconPath : Switch25DIconPath;
                if (!TryCreateSwitchIcon(marker.transform, iconPath))
                {
                    CreateMarkerLabel(marker.transform, kind == PetsCellKind.SwitchTo2D ? "2D" : "2.5D", 0.14f);
                }
            }
            else
            {
                if (!TryCreatePortalVisual(marker.transform, "Portal", new Vector3(0f, 0.1f, -0.05f), Quaternion.identity, 22))
                {
                    CreateMarkerLabel(marker.transform, "EXIT", 0.14f);
                }
            }

            return marker;
        }

        private bool TryCreatePortalVisual(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, int sortingOrder)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Portal2DVisualPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = Vector3.one * (cellSize * Portal2DVisualScale);
            ConfigurePortalAnimation(instance, sortingOrder);
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static void ConfigurePortalAnimation(GameObject instance, int sortingOrder)
        {
            if (instance == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sortingOrder = sortingOrder;
            Sprite frame1 = AssetDatabase.LoadAssetAtPath<Sprite>(Portal2DFrame1Path);
            Sprite frame2 = AssetDatabase.LoadAssetAtPath<Sprite>(Portal2DFrame2Path);
            Sprite frame3 = AssetDatabase.LoadAssetAtPath<Sprite>(Portal2DFrame3Path);
            Sprite frame4 = AssetDatabase.LoadAssetAtPath<Sprite>(Portal2DFrame4Path);
            if (frame1 == null || frame2 == null || frame3 == null || frame4 == null)
            {
                return;
            }

            PetsSpriteFrameAnimator frameAnimator = instance.GetComponent<PetsSpriteFrameAnimator>();
            if (frameAnimator == null)
            {
                frameAnimator = instance.AddComponent<PetsSpriteFrameAnimator>();
            }

            frameAnimator.Configure(spriteRenderer, new[] { frame1, frame2, frame3, frame4 }, 5f);
        }
#endif

        private bool TryCreateSwitchIcon(Transform parent, string spritePath)
        {
#if UNITY_EDITOR
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                return false;
            }

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(parent);
            icon.transform.localPosition = new Vector3(0f, 0.1f, -0.05f);
            icon.transform.localRotation = Quaternion.identity;

            SpriteRenderer spriteRenderer = icon.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 24;

            Vector2 spriteWorldSize = sprite.bounds.size;
            float largestSide = Mathf.Max(spriteWorldSize.x, spriteWorldSize.y);
            float scale = SwitchIconWorldSize / Mathf.Max(0.001f, largestSide);
            icon.transform.localScale = Vector3.one * scale;
            return true;
#else
            return false;
#endif
        }

        private static TextMesh CreateMarkerLabel(Transform parent, string text, float characterSize)
        {
            TextMesh label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(parent);
            label.transform.localPosition = new Vector3(0f, 0.09f, -0.05f);
            label.transform.localRotation = Quaternion.identity;
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 48;
            label.color = Color.black;
            return label;
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

                bool isActiveInCurrentMap = IsMarkerActive(tile, GetMarker(tile.Coord), currentMode);
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

                SpriteRenderer icon = sharedObject.GetComponentInChildren<SpriteRenderer>();
                if (icon != null)
                {
                    icon.transform.localPosition = currentMode == PetsPerspectiveMode.TwoD
                        ? new Vector3(0f, 0.1f, -0.05f)
                        : new Vector3(0f, 0.155f, -0.04f);
                    icon.transform.localRotation = currentMode == PetsPerspectiveMode.TwoD
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
                || kind == PetsCellKind.BouncePad;
        }

        private static bool IsTwoDClosedShapeKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BlackRegion
                || kind == PetsCellKind.BouncePad;
        }

        private static bool IsTopDownSurfaceKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BouncePad;
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
