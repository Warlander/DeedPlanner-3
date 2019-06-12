using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class HeightmapHandle : MonoBehaviour
    {

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
                propertyBlock.SetColor("_Color", value);
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
