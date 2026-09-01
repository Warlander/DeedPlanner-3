using System.Xml;
using Warlander.Deedplanner.Persistence;
using UnityEngine;

namespace Warlander.Deedplanner.Domain
{
    
    public abstract class LevelEntity : TileEntity, IXmlSerializable
    {
        public int Level => Tile.FindLevelOfEntity(this);
        public abstract void Serialize(XmlDocument document, XmlElement localRoot);
    }
}
