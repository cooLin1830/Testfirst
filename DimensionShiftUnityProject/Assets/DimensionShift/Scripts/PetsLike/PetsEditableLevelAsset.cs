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

        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public float CellSize => Mathf.Max(0.1f, cellSize);
        public PetsGridCoord Spawn => ClampCoord(spawn);
        public IReadOnlyList<PetsEditableCell> Cells => cells;

        public void Resize(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            spawn = ClampCoord(spawn);
            cells.RemoveAll(cell => cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height);
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
            return index >= 0 ? cells[index].kind : PetsCellKind.Empty;
        }

        public void SetCell(int x, int y, PetsCellKind kind)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            int index = IndexOf(x, y);
            if (kind == PetsCellKind.Empty)
            {
                if (index >= 0)
                {
                    cells.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                cells[index] = new PetsEditableCell(x, y, kind);
                return;
            }

            cells.Add(new PetsEditableCell(x, y, kind));
        }

        public void Clear()
        {
            cells.Clear();
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

            PetsGridCoord safeSpawn = Spawn;
            if (definition.GetCell(safeSpawn) == PetsCellKind.Empty)
            {
                definition.SetCell(safeSpawn.x, safeSpawn.y, PetsCellKind.WhiteInterior);
            }

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

        private PetsGridCoord ClampCoord(PetsGridCoord coord)
        {
            return new PetsGridCoord(Mathf.Clamp(coord.x, 0, Width - 1), Mathf.Clamp(coord.y, 0, Height - 1));
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
}
