using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class EntityData : IXmlSerializable
    {
        public int Level { get; private set; }
        public EntityType Type { get; private set; }

        public bool IsGroundLevel => Level == 0 || Level == -1;
        public bool IsSurface => Level >= 0;
        public bool IsCave => Level < 0;

        public EntityData(int floor, EntityType type)
        {
            Level = floor;
            Type = type;
        }

        public virtual void Apply(Tile tile, Transform targetTransform)
        {
            targetTransform.localPosition = new Vector3(tile.X * 4, tile.SurfaceHeight * 0.1f + Level * 3f, tile.Y * 4);
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

            return Level == data.Level && Type == data.Type;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Level;
        }

        public override string ToString()
        {
            return "Entity level " + Level + " type " + Type;
        }
    }
}
