using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Unitydata
{
    public class UnityGround : MonoBehaviour
    {
        private Ground ground;
        private MeshCollider groundCollider;

        public Ground Ground {
            get {
                return ground;
            }
            set {
                ground = value;
                InitializeGround();
            }
        }

        private void Start()
        {
            gameObject.layer = LayerMasks.GroundLayer;
        }

        private void InitializeGround()
        {
            if (ground == null)
            {
                return;
            }

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
            meshRenderer.material.SetTexture("_MainTex", ground.Data.Tex3d.Texture);
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