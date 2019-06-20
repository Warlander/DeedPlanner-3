using System;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Cave
{
    public class Cave : TileEntity, IXMLSerializable
    {

        private Tile tile;
        private CaveData data;
        
        public override Tile Tile => tile;
        public override Materials Materials => null;

        public CaveData Data {
            get => data;
            set => data = value;
        }

        public void Initialize(Tile tile, CaveData data)
        {
            this.tile = tile;
            this.data = data;
            gameObject.layer = LayerMasks.GroundLayer;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            throw new NotImplementedException();
        }
    }
}
