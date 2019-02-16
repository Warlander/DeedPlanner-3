using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class GridTile : MonoBehaviour
    {

        public MeshCollider Collider { get; private set; }

        public void Initialize(Mesh mesh)
        {
            gameObject.layer = LayerMasks.TileLayer;
            Collider = gameObject.GetComponent<MeshCollider>();
            if (!Collider)
            {
                Collider = gameObject.AddComponent<MeshCollider>();
            }
            Collider.sharedMesh = mesh;
        }

    }
}
