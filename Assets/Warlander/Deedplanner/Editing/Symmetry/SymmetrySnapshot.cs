using System;

namespace Warlander.Deedplanner.Editing
{
    public readonly struct SymmetrySnapshot
    {
        private readonly int _verticalAxisCoordinate2;
        private readonly int _horizontalAxisCoordinate2;

        public bool ReflectX { get; }
        public bool ReflectY { get; }

        public SymmetrySnapshot(bool reflectX, int verticalAxisCoordinate2, bool reflectY,
            int horizontalAxisCoordinate2)
        {
            ReflectX = reflectX;
            ReflectY = reflectY;
            _verticalAxisCoordinate2 = verticalAxisCoordinate2;
            _horizontalAxisCoordinate2 = horizontalAxisCoordinate2;
        }

        public int ReflectCellX(int x) => _verticalAxisCoordinate2 - x - 1;
        public int ReflectCellY(int y) => _horizontalAxisCoordinate2 - y - 1;
        public int ReflectVertexX(int x) => _verticalAxisCoordinate2 - x;
        public int ReflectVertexY(int y) => _horizontalAxisCoordinate2 - y;
        public float ReflectWorldX(float x) => _verticalAxisCoordinate2 * 4f - x;
        public float ReflectWorldY(float y) => _horizontalAxisCoordinate2 * 4f - y;

        public void ForEachTransform(Action<SymmetryTransform> action)
        {
            action(SymmetryTransform.Identity);
            if (ReflectX)
            {
                action(SymmetryTransform.ReflectX);
            }
            if (ReflectY)
            {
                action(SymmetryTransform.ReflectY);
            }
            if (ReflectX && ReflectY)
            {
                action(SymmetryTransform.ReflectX | SymmetryTransform.ReflectY);
            }
        }

        public int TransformCellX(int x, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectX) != 0 ? ReflectCellX(x) : x;

        public int TransformCellY(int y, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectY) != 0 ? ReflectCellY(y) : y;

        public int TransformVertexX(int x, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectX) != 0 ? ReflectVertexX(x) : x;

        public int TransformVertexY(int y, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectY) != 0 ? ReflectVertexY(y) : y;

        public float TransformWorldX(float x, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectX) != 0 ? ReflectWorldX(x) : x;

        public float TransformWorldY(float y, SymmetryTransform transform) =>
            (transform & SymmetryTransform.ReflectY) != 0 ? ReflectWorldY(y) : y;
    }
}
