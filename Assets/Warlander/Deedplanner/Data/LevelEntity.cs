using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    
    public abstract class LevelEntity : TileEntity, IXmlSerializable
    {
        public int Level => Tile.FindLevelOfEntity(this);
        public abstract void Serialize(XmlDocument document, XmlElement localRoot);
    }
}
