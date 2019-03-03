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
    public class Floor : MonoBehaviour, ITileEntity
    {

        public FloorData Data { get; private set; }
        public EntityOrientation Orientation { get; private set; }
        public Materials Materials { get { return Data.Materials; } }

        public GameObject Model { get; private set; }

        public void Initialize(FloorData data, Mesh mesh)
        {
            gameObject.layer = LayerMasks.FloorRoofMask;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            Data = data;

            Model = Data.Model.CreateOrGetModel();
            Model.transform.SetParent(transform);
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
