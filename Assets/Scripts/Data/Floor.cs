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
    public class Floor : TileEntity
    {

        private Tile tile;

        public FloorData Data { get; private set; }
        public EntityOrientation Orientation { get; private set; }
        public override Materials Materials { get { return Data.Materials; } }

        public GameObject Model { get; private set; }

        public override Tile Tile {
            get {
                return tile;
            }
        }

        public void Initialize(Tile tile, FloorData data, EntityOrientation orientation)
        {
            this.tile = tile;

            gameObject.layer = LayerMasks.FloorRoofLayer;

            Data = data;
            Orientation = orientation;

            Model = Data.Model.CreateOrGetModel();
            Model.transform.SetParent(transform);
            if (orientation == EntityOrientation.Right)
            {
                Model.transform.localRotation = Quaternion.Euler(0, 90, 0);
                Model.transform.localPosition = new Vector3(0, 0, -4);
            }
            else if (orientation == EntityOrientation.Down)
            {
                Model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Model.transform.localPosition = new Vector3(-4, 0, -4);
            }
            else if (orientation == EntityOrientation.Left)
            {
                Model.transform.localRotation = Quaternion.Euler(0, 270, 0);
                Model.transform.localPosition = new Vector3(-4, 0, 0);
            }

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
