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

        private static Material gridMaterial;

        public MeshCollider Collider { get; private set; }

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public void Initialize(Mesh mesh)
        {
            gameObject.layer = LayerMasks.TileLayer;
            Collider = gameObject.GetComponent<MeshCollider>();
            if (!Collider)
            {
                Collider = gameObject.AddComponent<MeshCollider>();
            }
            Collider.sharedMesh = mesh;

            if (!gridMaterial)
            {
                gridMaterial = new Material(Shader.Find("Standard"));
                gridMaterial.EnableKeyword("_ALPHATEST_ON");
                gridMaterial.color = new Color(0, 0, 0, 0);
            }
            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = gridMaterial;
            }
            if (!meshFilter)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }
        }

    }
}
