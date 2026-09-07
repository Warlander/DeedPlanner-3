using System.Collections.Generic;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveDirtyRegion
    {
        private readonly HashSet<CaveCellCoordinate> _cells = new HashSet<CaveCellCoordinate>();

        public int CellCount => _cells.Count;

        public bool ContainsCell(int x, int y)
        {
            return _cells.Contains(new CaveCellCoordinate(x, y));
        }

        public bool TouchesChunk(int minimumX, int minimumY, int width, int height)
        {
            int maximumX = minimumX + width;
            int maximumY = minimumY + height;
            foreach (CaveCellCoordinate cell in _cells)
            {
                if (cell.X >= minimumX && cell.X < maximumX && cell.Y >= minimumY && cell.Y < maximumY)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Add(int x, int y)
        {
            _cells.Add(new CaveCellCoordinate(x, y));
        }
    }

    internal readonly struct CaveCellCoordinate
    {
        public int X { get; }
        public int Y { get; }

        public CaveCellCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is CaveCellCoordinate other && X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }
    }
}
