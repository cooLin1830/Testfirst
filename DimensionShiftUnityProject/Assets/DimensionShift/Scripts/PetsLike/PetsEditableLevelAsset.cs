using System;
using System.Collections.Generic;
using UnityEngine;

namespace DimensionShift.PetsLike
{
    [CreateAssetMenu(menuName = "Dimension Shift/PETS Editable Level", fileName = "PETS Editable Level")]
    public sealed class PetsEditableLevelAsset : ScriptableObject
    {
        [SerializeField] private int width = 40;
        [SerializeField] private int height = 16;
        [SerializeField] private float cellSize = 1.15f;
        [SerializeField] private PetsGridCoord spawn = new PetsGridCoord(2, 2);
        [SerializeField] private List<PetsEditableCell> cells = new List<PetsEditableCell>();
        [SerializeField] private List<PetsEditableProp> props = new List<PetsEditableProp>();
        [SerializeField] private List<PetsEditableStar> stars = new List<PetsEditableStar>();
        [SerializeField] private List<PetsEditableMarker> markers = new List<PetsEditableMarker>();

        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public float CellSize => Mathf.Max(0.1f, cellSize);
        public PetsGridCoord Spawn => ClampCoord(spawn);
        public IReadOnlyList<PetsEditableCell> Cells => cells;
        public IReadOnlyList<PetsEditableProp> Props => props;
        public IReadOnlyList<PetsEditableStar> Stars => stars;
        public IReadOnlyList<PetsEditableMarker> Markers => markers;

        public void Resize(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            spawn = ClampCoord(spawn);
            cells.RemoveAll(cell => cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height);
            props.RemoveAll(prop => prop.x < 0 || prop.y < 0 || prop.x >= width || prop.y >= height);
            stars.RemoveAll(star => star.x < 0 || star.y < 0 || star.x >= width || star.y >= height);
            markers.RemoveAll(marker => marker.x < 0 || marker.y < 0 || marker.x >= width || marker.y >= height);
        }

        public void SetCellSize(float newCellSize)
        {
            cellSize = Mathf.Max(0.1f, newCellSize);
        }

        public void SetSpawn(PetsGridCoord coord)
        {
            spawn = ClampCoord(coord);
        }

        public PetsCellKind GetCell(int x, int y)
        {
            int index = IndexOf(x, y);
            if (index < 0)
            {
                return PetsCellKind.Empty;
            }

            PetsCellKind kind = cells[index].kind;
            if (IsLegacyPropCell(kind))
            {
                return PetsCellKind.WhiteInterior;
            }

            if (IsMarkerKind(kind))
            {
                return ShouldRestoreTerrainForLegacyMarker(x, y, kind) ? PetsCellKind.WhiteInterior : PetsCellKind.Empty;
            }

            return kind;
        }

        public void SetCell(int x, int y, PetsCellKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

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

                if (GetCell(x, y) == PetsCellKind.Empty)
                {
                    SetCell(x, y, PetsCellKind.WhiteInterior);
                }

                return;
            }

            int index = IndexOf(x, y);
            if (kind == PetsCellKind.Empty)
            {
                if (index >= 0)
                {
                    cells.RemoveAt(index);
                }

                SetProp(x, y, PetsPropKind.None);
                SetStar(x, y, false);
                SetMarker(x, y, PetsCellKind.Empty);
                return;
            }

            if (index >= 0)
            {
                cells[index] = new PetsEditableCell(x, y, kind);
                return;
            }

            cells.Add(new PetsEditableCell(x, y, kind));
        }

        public PetsPropKind GetProp(int x, int y)
        {
            int propIndex = PropIndexOf(x, y);
            if (propIndex >= 0)
            {
                PetsPropKind propKind = props[propIndex].kind;
                return propKind == PetsPropKind.Star ? PetsPropKind.None : propKind;
            }

            int cellIndex = IndexOf(x, y);
            if (cellIndex >= 0 && TryConvertLegacyPropCell(cells[cellIndex].kind, out PetsPropKind legacyProp) && legacyProp != PetsPropKind.Star)
            {
                return legacyProp;
            }

            return PetsPropKind.None;
        }

        public PetsCellKind GetMarker(int x, int y)
        {
            int markerIndex = MarkerIndexOf(x, y);
            if (markerIndex >= 0)
            {
                return markers[markerIndex].kind;
            }

            int cellIndex = IndexOf(x, y);
            if (cellIndex >= 0 && IsMarkerKind(cells[cellIndex].kind))
            {
                return cells[cellIndex].kind;
            }

            return PetsCellKind.Empty;
        }

        public void SetProp(int x, int y, PetsPropKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            if (kind == PetsPropKind.Star)
            {
                SetStar(x, y, true);
                return;
            }

            int index = PropIndexOf(x, y);
            if (kind == PetsPropKind.None)
            {
                if (index >= 0 && props[index].kind != PetsPropKind.Star)
                {
                    props.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                if (props[index].kind == PetsPropKind.Star)
                {
                    EnsureStarRecord(x, y);
                    props.Add(new PetsEditableProp(x, y, kind));
                    return;
                }

                props[index] = new PetsEditableProp(x, y, kind);
                return;
            }

            props.Add(new PetsEditableProp(x, y, kind));
        }

        public bool HasStar(int x, int y)
        {
            if (StarIndexOf(x, y) >= 0)
            {
                return true;
            }

            int propIndex = PropIndexOf(x, y);
            if (propIndex >= 0 && props[propIndex].kind == PetsPropKind.Star)
            {
                return true;
            }

            int cellIndex = IndexOf(x, y);
            return cellIndex >= 0 && cells[cellIndex].kind == PetsCellKind.Star;
        }

        public void SetStar(int x, int y, bool hasStar)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            if (!hasStar)
            {
                RemoveStarRecord(x, y);
                return;
            }

            EnsureStarRecord(x, y);
        }

        public void SetMarker(int x, int y, PetsCellKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            int index = MarkerIndexOf(x, y);
            if (kind == PetsCellKind.Empty)
            {
                if (index >= 0)
                {
                    markers.RemoveAt(index);
                }

                return;
            }

            if (!IsMarkerKind(kind))
            {
                return;
            }

            if (index >= 0)
            {
                markers[index] = new PetsEditableMarker(x, y, kind);
                return;
            }

            markers.Add(new PetsEditableMarker(x, y, kind));
        }

        public void Clear()
        {
            cells.Clear();
            props.Clear();
            stars.Clear();
            markers.Clear();
        }

        public PetsLevelDefinition ToLevelDefinition()
        {
            PetsLevelDefinition definition = new PetsLevelDefinition(Width, Height, CellSize)
            {
                Spawn = Spawn
            };

            for (int i = 0; i < cells.Count; i++)
            {
                PetsEditableCell cell = cells[i];
                if (cell.x < 0 || cell.y < 0 || cell.x >= Width || cell.y >= Height)
                {
                    continue;
                }

                if (IsMarkerKind(cell.kind))
                {
                    definition.SetMarker(cell.x, cell.y, cell.kind);
                    if (ShouldRestoreTerrainForLegacyMarker(cell.x, cell.y, cell.kind))
                    {
                        definition.SetCell(cell.x, cell.y, PetsCellKind.WhiteInterior);
                    }
                }
                else if (cell.kind == PetsCellKind.Star)
                {
                    definition.SetStar(cell.x, cell.y, true);
                    if (definition.GetCell(cell.x, cell.y) == PetsCellKind.Empty)
                    {
                        definition.SetCell(cell.x, cell.y, PetsCellKind.WhiteInterior);
                    }
                }
                else
                {
                    definition.SetCell(cell.x, cell.y, cell.kind);
                }
            }

            for (int i = 0; i < props.Count; i++)
            {
                PetsEditableProp prop = props[i];
                if (prop.x < 0 || prop.y < 0 || prop.x >= Width || prop.y >= Height)
                {
                    continue;
                }

                if (prop.kind == PetsPropKind.Star)
                {
                    definition.SetStar(prop.x, prop.y, true);
                }
                else
                {
                    definition.SetProp(prop.x, prop.y, prop.kind);
                }

                if (definition.GetCell(prop.x, prop.y) == PetsCellKind.Empty)
                {
                    definition.SetCell(prop.x, prop.y, PetsCellKind.WhiteInterior);
                }
            }

            for (int i = 0; i < stars.Count; i++)
            {
                PetsEditableStar star = stars[i];
                if (star.x < 0 || star.y < 0 || star.x >= Width || star.y >= Height)
                {
                    continue;
                }

                definition.SetStar(star.x, star.y, true);
                if (definition.GetCell(star.x, star.y) == PetsCellKind.Empty)
                {
                    definition.SetCell(star.x, star.y, PetsCellKind.WhiteInterior);
                }
            }

            for (int i = 0; i < markers.Count; i++)
            {
                PetsEditableMarker marker = markers[i];
                if (marker.x < 0 || marker.y < 0 || marker.x >= Width || marker.y >= Height)
                {
                    continue;
                }

                definition.SetMarker(marker.x, marker.y, marker.kind);
            }

            RemoveLegacyIsolatedSpawnTerrain(definition, Spawn);

            return definition;
        }

        private int IndexOf(int x, int y)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                PetsEditableCell cell = cells[i];
                if (cell.x == x && cell.y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        private int PropIndexOf(int x, int y)
        {
            for (int i = 0; i < props.Count; i++)
            {
                PetsEditableProp prop = props[i];
                if (prop.x == x && prop.y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        private int StarIndexOf(int x, int y)
        {
            for (int i = 0; i < stars.Count; i++)
            {
                PetsEditableStar star = stars[i];
                if (star.x == x && star.y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnsureStarRecord(int x, int y)
        {
            if (StarIndexOf(x, y) < 0)
            {
                stars.Add(new PetsEditableStar(x, y));
            }

            int propIndex = PropIndexOf(x, y);
            if (propIndex >= 0 && props[propIndex].kind == PetsPropKind.Star)
            {
                props.RemoveAt(propIndex);
            }

            int cellIndex = IndexOf(x, y);
            if (cellIndex >= 0 && cells[cellIndex].kind == PetsCellKind.Star)
            {
                cells[cellIndex] = new PetsEditableCell(x, y, PetsCellKind.WhiteInterior);
            }

            if (GetCell(x, y) == PetsCellKind.Empty)
            {
                SetCell(x, y, PetsCellKind.WhiteInterior);
            }
        }

        private void RemoveStarRecord(int x, int y)
        {
            int starIndex = StarIndexOf(x, y);
            if (starIndex >= 0)
            {
                stars.RemoveAt(starIndex);
            }

            int propIndex = PropIndexOf(x, y);
            if (propIndex >= 0 && props[propIndex].kind == PetsPropKind.Star)
            {
                props.RemoveAt(propIndex);
            }

            int cellIndex = IndexOf(x, y);
            if (cellIndex >= 0 && cells[cellIndex].kind == PetsCellKind.Star)
            {
                cells[cellIndex] = new PetsEditableCell(x, y, PetsCellKind.WhiteInterior);
            }
        }

        private int MarkerIndexOf(int x, int y)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                PetsEditableMarker marker = markers[i];
                if (marker.x == x && marker.y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        private PetsGridCoord ClampCoord(PetsGridCoord coord)
        {
            return new PetsGridCoord(Mathf.Clamp(coord.x, 0, Width - 1), Mathf.Clamp(coord.y, 0, Height - 1));
        }

        private void RemoveLegacyIsolatedSpawnTerrain(PetsLevelDefinition definition, PetsGridCoord safeSpawn)
        {
            if (GetProp(safeSpawn.x, safeSpawn.y) != PetsPropKind.None
                || definition.GetCell(safeSpawn) != PetsCellKind.WhiteInterior
                || HasNeighboringTerrain(definition, safeSpawn))
            {
                return;
            }

            definition.SetCell(safeSpawn.x, safeSpawn.y, PetsCellKind.Empty);
        }

        private static bool HasNeighboringTerrain(PetsLevelDefinition definition, PetsGridCoord coord)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (definition.GetCell(coord + new PetsGridCoord(x, y)) != PetsCellKind.Empty)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldRestoreTerrainForLegacyMarker(int x, int y, PetsCellKind markerKind)
        {
            int requiredNeighborCount = markerKind == PetsCellKind.Exit ? 1 : 2;
            return CountCardinalRawTerrainNeighbors(x, y) >= requiredNeighborCount;
        }

        private bool HasRawTerrainCell(int x, int y)
        {
            int index = IndexOf(x, y);
            if (index < 0)
            {
                return false;
            }

            PetsCellKind kind = cells[index].kind;
            return kind == PetsCellKind.WhiteInterior
                || kind == PetsCellKind.WhiteLine
                || kind == PetsCellKind.BlackRegion
                || kind == PetsCellKind.BouncePad
                || IsLegacyPropCell(kind);
        }

        private int CountCardinalRawTerrainNeighbors(int x, int y)
        {
            int count = 0;
            if (HasRawTerrainCell(x - 1, y))
            {
                count++;
            }

            if (HasRawTerrainCell(x + 1, y))
            {
                count++;
            }

            if (HasRawTerrainCell(x, y - 1))
            {
                count++;
            }

            if (HasRawTerrainCell(x, y + 1))
            {
                count++;
            }

            return count;
        }

        private static bool IsLegacyPropCell(PetsCellKind kind)
        {
            return kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox
                || kind == PetsCellKind.HeadBreakBox
                || kind == PetsCellKind.Star;
        }

        private static bool IsMarkerKind(PetsCellKind kind)
        {
            return kind == PetsCellKind.SwitchTo2D
                || kind == PetsCellKind.SwitchToTwoPointFiveD
                || kind == PetsCellKind.Exit;
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
    }

    [Serializable]
    public struct PetsEditableCell
    {
        public int x;
        public int y;
        public PetsCellKind kind;

        public PetsEditableCell(int x, int y, PetsCellKind kind)
        {
            this.x = x;
            this.y = y;
            this.kind = kind;
        }
    }

    [Serializable]
    public struct PetsEditableProp
    {
        public int x;
        public int y;
        public PetsPropKind kind;

        public PetsEditableProp(int x, int y, PetsPropKind kind)
        {
            this.x = x;
            this.y = y;
            this.kind = kind;
        }
    }

    [Serializable]
    public struct PetsEditableStar
    {
        public int x;
        public int y;

        public PetsEditableStar(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Serializable]
    public struct PetsEditableMarker
    {
        public int x;
        public int y;
        public PetsCellKind kind;

        public PetsEditableMarker(int x, int y, PetsCellKind kind)
        {
            this.x = x;
            this.y = y;
            this.kind = kind;
        }
    }
}
