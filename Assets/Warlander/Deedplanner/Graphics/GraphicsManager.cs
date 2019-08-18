using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class GraphicsManager : MonoBehaviour
    {

        public static GraphicsManager Instance;

        [SerializeField] private Material textureDefaultMaterial = null;
        [SerializeField] private Material womDefaultMaterial = null;
        [SerializeField] private Material simpleDrawingMaterial = null;
        [SerializeField] private Material ghostMaterial = null;

        public Material TextureDefaultMaterial => textureDefaultMaterial;
        public Material WomDefaultMaterial => womDefaultMaterial;
        public Material SimpleDrawingMaterial => simpleDrawingMaterial;
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
