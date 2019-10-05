using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    public class OverlayMesh : MonoBehaviour
    {
        
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        public void Initialize(Mesh targetMesh)
        {
            gameObject.layer = LayerMasks.TileLayer;

            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            
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