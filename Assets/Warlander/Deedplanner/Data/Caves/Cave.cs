using System.Xml;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Caves
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
