using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class EntityData : IXMLSerializable
    {

        public int Floor { get; private set; }
        public EntityType Type { get; private set; }

        public bool IsGroundFloor => Floor == 0 || Floor == -1;
        public bool IsSurface => Floor >= 0;
        public bool IsCave => Floor < 0;

        public EntityData(int floor, EntityType type)
        {
            Floor = floor;
            Type = type;
        }

        public virtual void Apply(Tile tile, Transform targetTransform)
        {
            targetTransform.localPosition = new Vector3(tile.X * 4, tile.SurfaceHeight * 0.1f + Floor * 3f, tile.Y * 4);
        }

        public virtual void Serialize(XmlDocument document, XmlElement localRoot)
        {
            // no extra data needed
        }
        
        public override bool Equals(object other)
        {
            EntityData data = other as EntityData;
            
            if (data == null)
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Floor;
        }

        public override string ToString()
        {
            return "Entity floor " + Floor + " type " + Type;
        }
    }
}
