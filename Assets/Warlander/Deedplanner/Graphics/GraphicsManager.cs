using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class GraphicsManager : MonoBehaviour
    {

        public static GraphicsManager Instance;

        [SerializeField] private Material textureDefaultMaterial = null;
        [SerializeField] private Material womDefaultMaterial = null;
        [SerializeField] private Material simpleDrawingMaterial = null;

        [SerializeField] private Projector axisProjectorPrefab = null;
        
        public Material TextureDefaultMaterial => textureDefaultMaterial;
        public Material WomDefaultMaterial => womDefaultMaterial;
        public Material SimpleDrawingMaterial => simpleDrawingMaterial;

        public Projector AxisProjectorPrefab => axisProjectorPrefab;

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
