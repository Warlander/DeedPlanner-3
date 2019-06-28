using System.Globalization;
using System.Xml;

namespace Warlander.Deedplanner.Data.Objects
{
    public class ObjectEntityData : EntityData
    {

        public float X { get; }
        public float Y { get; }

        public ObjectEntityData(int floor, EntityType type, float x, float y) : base(floor, type)
        {
            X = x;
            Y = y;
        }

        public new void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("x", X.ToString(CultureInfo.InvariantCulture));
            localRoot.SetAttribute("y", Y.ToString(CultureInfo.InvariantCulture));
        }
        
        public override bool Equals(object other)
        {
            ObjectEntityData data = other as ObjectEntityData;
            
            if (data == null)
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type && X == X && Y == Y;
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
