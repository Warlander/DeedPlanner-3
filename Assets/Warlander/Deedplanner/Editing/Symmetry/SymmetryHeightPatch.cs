using System;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Editing
{
    public sealed class SymmetryHeightPatch
    {
        private readonly Dictionary<Coordinate, Proposal> _proposals =
            new Dictionary<Coordinate, Proposal>();

        public void Clear() => _proposals.Clear();

        public void Add(SymmetrySnapshot snapshot, int mapWidth, int mapHeight,
            int x, int y, int value)
        {
            if (x < 0 || x > mapWidth || y < 0 || y > mapHeight)
            {
                return;
            }

            snapshot.ForEachTransform(transform =>
            {
                int targetX = snapshot.TransformVertexX(x, transform);
                int targetY = snapshot.TransformVertexY(y, transform);
                if (targetX < 0 || targetX > mapWidth || targetY < 0 || targetY > mapHeight)
                {
                    return;
                }

                var coordinate = new Coordinate(targetX, targetY);
                int priority = GetPriority(transform);
                if (!_proposals.TryGetValue(coordinate, out Proposal existing)
                    || priority < existing.Priority)
                {
                    _proposals[coordinate] = new Proposal(value, priority);
                }
            });
        }

        public void Apply(Action<int, int, int> action)
        {
            foreach (KeyValuePair<Coordinate, Proposal> pair in _proposals)
            {
                action(pair.Key.X, pair.Key.Y, pair.Value.Value);
            }
        }

        private static int GetPriority(SymmetryTransform transform)
        {
            switch (transform)
            {
                case SymmetryTransform.Identity:
                    return 0;
                case SymmetryTransform.ReflectX:
                    return 1;
                case SymmetryTransform.ReflectY:
                    return 2;
                default:
                    return 3;
            }
        }

        private readonly struct Coordinate : IEquatable<Coordinate>
        {
            public int X { get; }
            public int Y { get; }

            public Coordinate(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(Coordinate other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is Coordinate other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        private readonly struct Proposal
        {
            public int Value { get; }
            public int Priority { get; }

            public Proposal(int value, int priority)
            {
                Value = value;
                Priority = priority;
            }
        }
    }
}
