using System.Text;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Utils;

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
                propertyBlock.SetColor(ShaderPropertyIds.Color, value);
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

        public string ToRichString()
        {
            StringBuilder build = new StringBuilder();
            
            build.Append("X: " + TileCoords.x + " Y: " + TileCoords.y).AppendLine();
            
            build.Append("<mspace=0.5em>");

            int floor = LayoutManager.Instance.CurrentCamera.Floor;
            Map map = GameManager.Instance.Map;
            Tile centralTile = map[TileCoords.x, TileCoords.y];
            int centralHeight = centralTile.GetHeightForFloor(floor);
            for (int i = 1; i >= -1; i--)
            {
                for (int i2 = -1; i2 <= 1; i2++)
                {
                    if (i == 0 && i2 == 0)
                    {
                        build.Append("<b>").Append(StringUtils.PaddedNumberString(centralHeight, 5)).Append("</b>");
                    }
                    else
                    {
                        int heightDifference = centralHeight - TileHeightOrDefault(map[TileCoords.x + i2, TileCoords.y + i], centralHeight, floor);
                        build.Append(StringUtils.PaddedNumberString(heightDifference, 5));
                    }

                    if (i2 != 1)
                    {
                        build.Append(" ");
                    }
                }

                if (i != -1)
                {
                    build.Append("<br>");
                }
            }
                        
            build.Append("</mspace>");

            return build.ToString();
        }
        
        private static int TileHeightOrDefault(Tile tile, int defaultHeight, int floor)
        {
            return tile ? tile.GetHeightForFloor(floor) : defaultHeight;
        }

    }
}
