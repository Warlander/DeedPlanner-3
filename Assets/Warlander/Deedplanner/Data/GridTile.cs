using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class GridTile : MonoBehaviour
    {

        private static Material gridMaterial;

        public MeshCollider Collider { get; private set; }

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public int X { get; private set; }
        public int Y { get; private set; }

        public void Initialize(Mesh mesh, int x, int y)
        {
            X = x;
            Y = y;

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
                Material[] materials = new Material[4];
                for (int i = 0; i < 4; i++)
                {
                    materials[i] = gridMaterial;
                }

                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.materials = materials;
            }
            if (!meshFilter)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }
        }

    }
}
