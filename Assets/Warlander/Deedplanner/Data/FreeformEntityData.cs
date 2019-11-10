using System.Globalization;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class FreeformEntityData : EntityData
    {

        public float X { get; }
        public float Y { get; }
        public bool FloatOnWater { get; }

        public FreeformEntityData(int floor, EntityType type, float x, float y, bool floatOnWater) : base(floor, type)
        {
            X = x;
            Y = y;
            FloatOnWater = floatOnWater;
        }

        public override void Apply(Tile tile, Transform targetTransform)
        {
            float x = tile.X * 4 + X;
            float z = tile.Y * 4 + Y;
            float interpolatedHeight = tile.Map.GetInterpolatedHeight(x, z);
            if (FloatOnWater)
            {
                interpolatedHeight = Mathf.Max(interpolatedHeight, 0);
            }
            const float floorHeight = 0.25f;
            bool containsFloor = tile.GetTileContent(Floor);
            if (containsFloor)
            {
                interpolatedHeight += floorHeight;
            }
            targetTransform.localPosition = new Vector3(x, interpolatedHeight + Floor * 3f, z);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("x", X.ToString(CultureInfo.InvariantCulture));
            localRoot.SetAttribute("y", Y.ToString(CultureInfo.InvariantCulture));
        }
        
        public override bool Equals(object other)
        {
            if (!(other is FreeformEntityData data))
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type && X == data.X && Y == data.Y;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Floor + (int)(X * 1000000) + (int)(Y * 100000000);
        }

        public override string ToString()
        {
            return "Entity floor " + Floor + " type " + Type + " X " + X + " Y " + Y;
        }

    }
}
