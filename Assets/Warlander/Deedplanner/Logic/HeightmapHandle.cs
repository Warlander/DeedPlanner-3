using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class HeightmapHandle : MonoBehaviour
    {

        private static readonly int Color1 = Shader.PropertyToID("_Color");
        
        public Vector2Int TileCoords { get; private set; }

        private Renderer commonRenderer;
        private Color color;

        public Color Color {
            get => color;
            set {
                if (color == value)
                {
                    return;
                }
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(Color1, value);
                commonRenderer.SetPropertyBlock(propertyBlock);
                color = value;
            }
        }

        public void Initialize(Vector2Int tileCoords)
        {
            name = "Handle " + tileCoords.x + "X" + tileCoords.y;
            TileCoords = tileCoords;

            commonRenderer = GetComponent<Renderer>();
            Color = Color.white;
        }

    }
}
