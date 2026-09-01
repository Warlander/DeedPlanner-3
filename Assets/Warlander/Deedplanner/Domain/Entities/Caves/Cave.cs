using System.Xml;
using Warlander.Deedplanner.Persistence;

namespace Warlander.Deedplanner.Domain.Entities.Caves
{
    public class Cave : IXmlSerializable
    {
        private CaveData data;
        
        public Tile Tile { get; }

        public CaveData Data {
            get => data;
            set => data = value;
        }

        public Cave(Tile tile, CaveData data)
        {
            Tile = tile;
            this.data = data;
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", data.ShortName);
        }
    }
}
