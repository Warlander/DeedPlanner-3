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
    public class Wall : TileEntity
    {

        public WallData Data { get; private set; }
        public bool Reversed { get; private set; }
        public override Materials Materials { get { return Data.Materials; } }

        public GameObject Model { get; private set; }

        public void Initialize(WallData data, bool reversed, bool firstFloor)
        {
            gameObject.layer = LayerMasks.WallLayer;

            Data = data;

            if (firstFloor)
            {
                Model = Data.BottomModel.CreateOrGetModel();
            }
            else
            {
                Model = Data.NormalModel.CreateOrGetModel();
            }
            Model.transform.SetParent(transform);

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, 1.5f, 0);
                collider.size = new Vector3(4f, 3f, 0.1f);
            }

            //transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
