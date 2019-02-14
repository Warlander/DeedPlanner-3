using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class Ground : MonoBehaviour
    {

        private GroundData data;

        private MeshRenderer meshRenderer;
        private MeshCollider groundCollider;

        public RoadDirection RoadDirection { get; private set; }

        public GroundData Data {
            get {
                return data;
            }
            set {
                data = value;
                meshRenderer.material = data.Tex3d.Material;
            }
        }

        public void Initialize(GroundData data, Mesh mesh)
        {
            gameObject.layer = LayerMasks.GroundLayer;
            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            if (!GetComponent<MeshFilter>())
            {
                gameObject.AddComponent<MeshFilter>();
            }

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            if (!groundCollider)
            {
                groundCollider = gameObject.AddComponent<MeshCollider>();
            }
            groundCollider.sharedMesh = mesh;

            Data = data;
            RoadDirection = RoadDirection.Center;
        }

    }
}
