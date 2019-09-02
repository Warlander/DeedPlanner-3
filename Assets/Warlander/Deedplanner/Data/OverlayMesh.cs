using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class OverlayMesh : MonoBehaviour
    {
        
        private static Material gridMaterial;
        
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        public void Initialize(Mesh targetMesh)
        {
            gameObject.layer = LayerMasks.TileLayer;
            if (!gridMaterial)
            {
                gridMaterial = new Material(Shader.Find("Standard"));
                gridMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                gridMaterial.EnableKeyword("_ALPHATEST_ON");
                gridMaterial.renderQueue = 2450;
                gridMaterial.color = new Color(0, 0, 0, 0);
            }
            
            meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            meshCollider = GetComponent<MeshCollider>();
            if (!meshCollider)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshRenderer.sharedMaterial = gridMaterial;
            meshFilter.sharedMesh = targetMesh;
            meshCollider.sharedMesh = targetMesh;
        }

        public void UpdateCollider()
        {
            // turning collider off and on to force it to update
            meshCollider.enabled = false;
            // ReSharper disable once Unity.InefficientPropertyAccess
            meshCollider.enabled = true;
        }

    }
}