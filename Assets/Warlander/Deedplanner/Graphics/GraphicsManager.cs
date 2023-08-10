using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class GraphicsManager : MonoBehaviour
    {
        public static GraphicsManager Instance;
        
        [SerializeField] private Material womDefaultMaterial = null;
        [SerializeField] private Material simpleDrawingMaterial = null;
        [SerializeField] private Material simpleSubtleDrawingMaterial = null;
        [SerializeField] private Material terrainMaterial = null;
        [SerializeField] private Material ghostMaterial = null;
        
        public Material WomDefaultMaterial => womDefaultMaterial;
        public Material SimpleDrawingMaterial => simpleDrawingMaterial;
        public Material SimpleSubtleDrawingMaterial => simpleSubtleDrawingMaterial;
        public Material TerrainMaterial => terrainMaterial;
        public Material GhostMaterial => ghostMaterial;

        public GraphicsManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}
