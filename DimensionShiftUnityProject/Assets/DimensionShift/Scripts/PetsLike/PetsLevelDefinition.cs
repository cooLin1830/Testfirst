using System.Collections.Generic;
using UnityEngine;

namespace DimensionShift.PetsLike
{
    public sealed class PetsLevelDefinition
    {
        private readonly Dictionary<Vector2Int, PetsCellKind> twoDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsCellKind> twoPointFiveDCells = new Dictionary<Vector2Int, PetsCellKind>();
        private readonly Dictionary<Vector2Int, PetsPropKind> props = new Dictionary<Vector2Int, PetsPropKind>();
        private readonly HashSet<Vector2Int> stars = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, PetsCellKind> markers = new Dictionary<Vector2Int, PetsCellKind>();
        private bool hasTwoPointFiveDOverride;

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public PetsGridCoord Spawn { get; set; }

        public IEnumerable<KeyValuePair<Vector2Int, PetsCellKind>> Cells => twoDCells;
        public IEnumerable<KeyValuePair<Vector2Int, PetsCellKind>> TwoDCells => twoDCells;
        public IEnumerable<KeyValuePair<Vector2Int, PetsCellKind>> TwoPointFiveDCells => hasTwoPointFiveDOverride ? twoPointFiveDCells : twoDCells;
        public IEnumerable<KeyValuePair<Vector2Int, PetsPropKind>> Props => props;
        public IEnumerable<Vector2Int> Stars => stars;
        public IEnumerable<KeyValuePair<Vector2Int, PetsCellKind>> Markers => markers;

        public PetsLevelDefinition(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            Spawn = new PetsGridCoord(1, 1);
        }

        public void SetCell(int x, int y, PetsCellKind kind)
        {
            if (IsMarkerKind(kind))
            {
                SetMarker(x, y, kind);
                return;
            }

            if (TryConvertLegacyPropCell(kind, out PetsPropKind propKind))
            {
                if (propKind == PetsPropKind.Star)
                {
                    SetStar(x, y, true);
                }
                else
                {
                    SetProp(x, y, propKind);
                }

                EnsureTerrainForProp(twoDCells, x, y);
                if (!hasTwoPointFiveDOverride)
                {
                    EnsureTerrainForProp(twoPointFiveDCells, x, y);
                }

                return;
            }

            SetCellRaw(twoDCells, x, y, kind);
            if (!hasTwoPointFiveDOverride)
            {
                SetCellRaw(twoPointFiveDCells, x, y, kind);
            }

            if (kind == PetsCellKind.Empty)
            {
                SetProp(x, y, PetsPropKind.None);
                SetStar(x, y, false);
                SetMarker(x, y, PetsCellKind.Empty);
            }
        }

        public void SetCell(int x, int y, PetsCellKind kind, PetsPerspectiveMode mode)
        {
            if (IsMarkerKind(kind))
            {
                SetMarker(x, y, kind);
                return;
            }

            if (TryConvertLegacyPropCell(kind, out PetsPropKind propKind))
            {
                if (propKind == PetsPropKind.Star)
                {
                    SetStar(x, y, true);
                }
                else
                {
                    SetProp(x, y, propKind);
                }

                if (mode == PetsPerspectiveMode.TwoPointFiveD)
                {
                    EnsureTwoPointFiveDOverride();
                    EnsureTerrainForProp(twoPointFiveDCells, x, y);
                }
                else
                {
                    EnsureTerrainForProp(twoDCells, x, y);
                    if (!hasTwoPointFiveDOverride)
                    {
                        EnsureTerrainForProp(twoPointFiveDCells, x, y);
                    }
                }

                return;
            }

            if (mode == PetsPerspectiveMode.TwoPointFiveD)
            {
                EnsureTwoPointFiveDOverride();
                SetCellRaw(twoPointFiveDCells, x, y, kind);
                if (kind == PetsCellKind.Empty)
                {
                    SetMarker(x, y, PetsCellKind.Empty);
                }

                return;
            }

            SetCell(x, y, kind);
        }

        public void SetMarker(int x, int y, PetsCellKind kind)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (kind == PetsCellKind.Empty)
            {
                markers.Remove(coord);
                return;
            }

            if (!IsMarkerKind(kind))
            {
                return;
            }

            markers[coord] = kind;
        }

        public void SetProp(int x, int y, PetsPropKind kind)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (kind == PetsPropKind.Star)
            {
                SetStar(x, y, true);
                return;
            }

            if (kind == PetsPropKind.None)
            {
                props.Remove(coord);
                return;
            }

            props[coord] = kind;
            EnsureTerrainForProp(twoDCells, x, y);
            EnsureTerrainForProp(twoPointFiveDCells, x, y);
        }

        public void SetStar(int x, int y, bool hasStar)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (!hasStar)
            {
                stars.Remove(coord);
                return;
            }

            stars.Add(coord);
            EnsureTerrainForProp(twoDCells, x, y);
            EnsureTerrainForProp(twoPointFiveDCells, x, y);
        }

        public void FillRect(RectInt rect, PetsCellKind kind)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    SetCell(x, y, kind);
                }
            }
        }

        public void FillRect(RectInt rect, PetsCellKind kind, PetsPerspectiveMode mode)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    SetCell(x, y, kind, mode);
                }
            }
        }

        public void DrawRectOutline(RectInt rect, PetsCellKind kind)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                SetCell(x, rect.yMin, kind);
                SetCell(x, rect.yMax - 1, kind);
            }

            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                SetCell(rect.xMin, y, kind);
                SetCell(rect.xMax - 1, y, kind);
            }
        }

        public PetsCellKind GetCell(PetsGridCoord coord)
        {
            return GetCell(coord.x, coord.y);
        }

        public PetsCellKind GetCell(int x, int y)
        {
            return GetCell(x, y, PetsPerspectiveMode.TwoD);
        }

        public PetsCellKind GetCell(PetsGridCoord coord, PetsPerspectiveMode mode)
        {
            return GetCell(coord.x, coord.y, mode);
        }

        public PetsCellKind GetCell(int x, int y, PetsPerspectiveMode mode)
        {
            Dictionary<Vector2Int, PetsCellKind> source = mode == PetsPerspectiveMode.TwoPointFiveD && hasTwoPointFiveDOverride
                ? twoPointFiveDCells
                : twoDCells;
            if (source.TryGetValue(new Vector2Int(x, y), out PetsCellKind kind))
            {
                return kind;
            }

            return PetsCellKind.Empty;
        }

        public PetsPropKind GetProp(PetsGridCoord coord)
        {
            return GetProp(coord.x, coord.y);
        }

        public PetsPropKind GetProp(int x, int y)
        {
            return props.TryGetValue(new Vector2Int(x, y), out PetsPropKind kind) ? kind : PetsPropKind.None;
        }

        public bool HasStar(PetsGridCoord coord)
        {
            return HasStar(coord.x, coord.y);
        }

        public bool HasStar(int x, int y)
        {
            return stars.Contains(new Vector2Int(x, y));
        }

        public PetsCellKind GetMarker(PetsGridCoord coord)
        {
            return GetMarker(coord.x, coord.y);
        }

        public PetsCellKind GetMarker(int x, int y)
        {
            return markers.TryGetValue(new Vector2Int(x, y), out PetsCellKind kind) ? kind : PetsCellKind.Empty;
        }

        public bool Contains(PetsGridCoord coord)
        {
            return coord.x >= 0 && coord.y >= 0 && coord.x < Width && coord.y < Height;
        }

        private static void SetCellRaw(Dictionary<Vector2Int, PetsCellKind> target, int x, int y, PetsCellKind kind)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (kind == PetsCellKind.Empty)
            {
                target.Remove(coord);
                return;
            }

            target[coord] = kind;
        }

        private static void EnsureTerrainForProp(Dictionary<Vector2Int, PetsCellKind> target, int x, int y)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (!target.ContainsKey(coord))
            {
                target[coord] = PetsCellKind.WhiteInterior;
            }
        }

        private static bool TryConvertLegacyPropCell(PetsCellKind kind, out PetsPropKind propKind)
        {
            switch (kind)
            {
                case PetsCellKind.BreakableBrick:
                    propKind = PetsPropKind.BreakableBrick;
                    return true;
                case PetsCellKind.PushBox:
                    propKind = PetsPropKind.PushBox;
                    return true;
                case PetsCellKind.HeadBreakBox:
                    propKind = PetsPropKind.HeadBreakBox;
                    return true;
                case PetsCellKind.Star:
                    propKind = PetsPropKind.Star;
                    return true;
                default:
                    propKind = PetsPropKind.None;
                    return false;
            }
        }

        private static bool IsMarkerKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit;
        }

        private void EnsureTwoPointFiveDOverride()
        {
            if (hasTwoPointFiveDOverride)
            {
                return;
            }

            hasTwoPointFiveDOverride = true;
            twoPointFiveDCells.Clear();
        }
    }
}
