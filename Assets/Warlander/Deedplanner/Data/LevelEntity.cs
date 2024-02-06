using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    
    public abstract class LevelEntity : TileEntity, IXmlSerializable
    {
        public int Floor => Tile.FindFloorOfEntity(this);
        public abstract void Serialize(XmlDocument document, XmlElement localRoot);
    }
}
