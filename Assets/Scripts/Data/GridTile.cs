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

        public void Initialize(Mesh mesh)
        {
            gameObject.layer = LayerMasks.TileLayer;
            MeshCollider tileCollider = gameObject.GetComponent<MeshCollider>();
            if (!tileCollider)
            {
                tileCollider = gameObject.AddComponent<MeshCollider>();
            }
            tileCollider.sharedMesh = mesh;
        }

    }
}
