using System;
using UnityEngine;

namespace DimensionShift.PetsLike
{
    [Serializable]
    public struct PetsGridCoord : IEquatable<PetsGridCoord>
    {
        public int x;
        public int y;

        public PetsGridCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static PetsGridCoord operator +(PetsGridCoord a, PetsGridCoord b)
        {
            return new PetsGridCoord(a.x + b.x, a.y + b.y);
        }

        public static PetsGridCoord operator -(PetsGridCoord a, PetsGridCoord b)
        {
            return new PetsGridCoord(a.x - b.x, a.y - b.y);
        }

        public static PetsGridCoord FromVector2Int(Vector2Int value)
        {
            return new PetsGridCoord(value.x, value.y);
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }

        public bool Equals(PetsGridCoord other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is PetsGridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}
