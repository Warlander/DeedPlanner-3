using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{

    public abstract class TileEntity : MonoBehaviour, IXMLSerializable
    {

        public abstract Tile Tile { get; }
        public abstract Materials Materials { get; }
        public int Floor => Tile.FindFloorOfEntity(this);

        public EntityType Type => Tile.FindTypeOfEntity(this);

        public abstract void Serialize(XmlDocument document, XmlElement localRoot);

    }

}
