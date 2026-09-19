using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public readonly struct SymmetryCellTarget
    {
        public int X { get; }
        public int Y { get; }
        public SymmetryTransform Transform { get; }

        public SymmetryCellTarget(int x, int y, SymmetryTransform transform)
        {
            X = x;
            Y = y;
            Transform = transform;
        }
    }

    public readonly struct SymmetryVertexTarget
    {
        public int X { get; }
        public int Y { get; }
        public SymmetryTransform Transform { get; }

        public SymmetryVertexTarget(int x, int y, SymmetryTransform transform)
        {
            X = x;
            Y = y;
            Transform = transform;
        }
    }

    public readonly struct SymmetryPoseTarget
    {
        public int TileX { get; }
        public int TileY { get; }
        public Vector2 LocalPosition { get; }
        public float Rotation { get; }
        public SymmetryTransform Transform { get; }

        public SymmetryPoseTarget(int tileX, int tileY, Vector2 localPosition, float rotation,
            SymmetryTransform transform)
        {
            TileX = tileX;
            TileY = tileY;
            LocalPosition = localPosition;
            Rotation = rotation;
            Transform = transform;
        }
    }
}
