using System.Xml;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Caves
{
    public class Cave : TileEntity, IXMLSerializable
    {
        
        private CaveData data;
        public override Materials Materials => null;

        public CaveData Data {
            get => data;
            set => data = value;
        }

        public void Initialize(Tile tile, CaveData data)
        {
            Tile = tile;
            this.data = data;
            gameObject.layer = LayerMasks.GroundLayer;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", data.ShortName);
        }
    }
}
