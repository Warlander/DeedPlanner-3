using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class Ground : MonoBehaviour
    {

        public GroundData Data { get; private set; }
        public RoadDirection RoadDirection { get; private set; }

        private MeshCollider groundCollider;

        public void Initialize(GroundData data)
        {
            Data = data;
            RoadDirection = RoadDirection.Center;

            if (!GetComponent<MeshRenderer>())
            {
                gameObject.AddComponent<MeshRenderer>();
            }
            if (!GetComponent<MeshFilter>())
            {
                gameObject.AddComponent<MeshFilter>();
            }

            Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4) };
            Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material.SetTexture("_MainTex", Data.Tex3d.Texture);
            meshRenderer.material.SetFloat("_Glossiness", 0);
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            if (!groundCollider)
            {
                groundCollider = gameObject.AddComponent<MeshCollider>();
            }
            groundCollider.sharedMesh = mesh;
        }

    }
}
