using System;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Grounds;

namespace Warlander.Deedplanner.Editing
{
    [Flags]
    public enum SymmetryTransform
    {
        Identity = 0,
        ReflectX = 1,
        ReflectY = 2
    }

    public static class SymmetryTransformExtensions
    {
        public static RoadDirection Transform(this SymmetryTransform transform, RoadDirection direction)
        {
            if ((transform & SymmetryTransform.ReflectX) != 0)
            {
                direction = direction switch
                {
                    RoadDirection.NW => RoadDirection.NE,
                    RoadDirection.NE => RoadDirection.NW,
                    RoadDirection.SW => RoadDirection.SE,
                    RoadDirection.SE => RoadDirection.SW,
                    _ => direction
                };
            }

            if ((transform & SymmetryTransform.ReflectY) != 0)
            {
                direction = direction switch
                {
                    RoadDirection.NW => RoadDirection.SW,
                    RoadDirection.NE => RoadDirection.SE,
                    RoadDirection.SW => RoadDirection.NW,
                    RoadDirection.SE => RoadDirection.NE,
                    _ => direction
                };
            }

            return direction;
        }

        public static EntityOrientation Transform(this SymmetryTransform transform, EntityOrientation orientation)
        {
            if ((transform & SymmetryTransform.ReflectX) != 0)
            {
                orientation = orientation switch
                {
                    EntityOrientation.Left => EntityOrientation.Right,
                    EntityOrientation.Right => EntityOrientation.Left,
                    _ => orientation
                };
            }

            if ((transform & SymmetryTransform.ReflectY) != 0)
            {
                orientation = orientation switch
                {
                    EntityOrientation.Up => EntityOrientation.Down,
                    EntityOrientation.Down => EntityOrientation.Up,
                    _ => orientation
                };
            }

            return orientation;
        }

        public static float TransformRotation(this SymmetryTransform transform, float rotation)
        {
            if ((transform & SymmetryTransform.ReflectX) != 0)
            {
                rotation = -rotation;
            }

            if ((transform & SymmetryTransform.ReflectY) != 0)
            {
                rotation = Mathf.PI - rotation;
            }

            rotation %= Mathf.PI * 2f;
            return rotation < 0f ? rotation + Mathf.PI * 2f : rotation;
        }
    }
}
