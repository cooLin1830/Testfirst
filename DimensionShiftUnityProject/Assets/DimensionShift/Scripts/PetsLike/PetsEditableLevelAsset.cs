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

        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public float CellSize => Mathf.Max(0.1f, cellSize);
        public PetsGridCoord Spawn => ClampCoord(spawn);
        public IReadOnlyList<PetsEditableCell> Cells => cells;
        public IReadOnlyList<PetsEditableProp> Props => props;

        public void Resize(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            spawn = ClampCoord(spawn);
            cells.RemoveAll(cell => cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height);
            props.RemoveAll(prop => prop.x < 0 || prop.y < 0 || prop.x >= width || prop.y >= height);
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
            return IsLegacyPropCell(kind) ? PetsCellKind.WhiteInterior : kind;
        }

        public void SetCell(int x, int y, PetsCellKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            if (TryConvertLegacyPropCell(kind, out PetsPropKind propKind))
            {
                SetProp(x, y, propKind);
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
                return props[propIndex].kind;
            }

            int cellIndex = IndexOf(x, y);
            if (cellIndex >= 0 && TryConvertLegacyPropCell(cells[cellIndex].kind, out PetsPropKind legacyProp))
            {
                return legacyProp;
            }

            return PetsPropKind.None;
        }

        public void SetProp(int x, int y, PetsPropKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            int index = PropIndexOf(x, y);
            if (kind == PetsPropKind.None)
            {
                if (index >= 0)
                {
                    props.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                props[index] = new PetsEditableProp(x, y, kind);
                return;
            }

            props.Add(new PetsEditableProp(x, y, kind));
        }

        public void Clear()
        {
            cells.Clear();
            props.Clear();
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

                definition.SetCell(cell.x, cell.y, cell.kind);
            }

            for (int i = 0; i < props.Count; i++)
            {
                PetsEditableProp prop = props[i];
                if (prop.x < 0 || prop.y < 0 || prop.x >= Width || prop.y >= Height)
                {
                    continue;
                }

                definition.SetProp(prop.x, prop.y, prop.kind);
                if (definition.GetCell(prop.x, prop.y) == PetsCellKind.Empty)
                {
                    definition.SetCell(prop.x, prop.y, PetsCellKind.WhiteInterior);
                }
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

        private static bool IsLegacyPropCell(PetsCellKind kind)
        {
            return kind == PetsCellKind.BreakableBrick
                || kind == PetsCellKind.PushBox
                || kind == PetsCellKind.HeadBreakBox;
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
}
