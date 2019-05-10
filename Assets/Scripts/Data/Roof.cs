using Assets.Scripts.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class Roof : TileEntity
    {

        private Tile tile;

        public RoofData Data { get; private set; }
        public RoofType RoofType { get; private set; }
        public override Materials Materials { get { return Data.Materials; } }

        public GameObject Model { get; private set; }

        public override Tile Tile {
            get {
                return tile;
            }
        }

        public void Initialize(Tile tile, RoofData data, RoofType roofType)
        {
            this.tile = tile;

            gameObject.layer = LayerMasks.FloorRoofLayer;

            Data = data;
            RoofType = roofType;

            Model = roofType.Model.CreateOrGetModel();
            Model.transform.SetParent(transform);

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(-2f, 0.125f, -2f);
                collider.size = new Vector3(4f, 0.25f, 4f);
            }

            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
