using System.Text;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Utils;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class HeightmapHandle
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private GameManager _gameManager;
        
        private const float handleScale = 0.6f;
        
        public Vector2Int TileCoords { get; }
        public Matrix4x4 TransformMatrix { get; private set; }
        
        private int slope;
        private Color color;

        public int Slope
        {
            get => slope;
            set
            {
                slope = value;
                TransformMatrix = Matrix4x4.TRS(WorldPosition, Quaternion.identity, Vector3.one * handleScale);
            }
        }

        private Vector3 WorldPosition
        {
            get
            {
                float x = TileCoords.x * 4;
                float y = slope * 0.1f;
                float z = TileCoords.y * 4;
                return new Vector3(x, y, z);
            }
        }

        public Color Color {
            get => color;
            set {
                if (color == value)
                {
                    return;
                }
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(ShaderPropertyIds.Color, value);
                color = value;
            }
        }

        public HeightmapHandle(Vector2Int tileCoords, int slope)
        {
            TileCoords = tileCoords;
            Color = Color.white;
            Slope = slope;
        }

        public float Raycast(Ray ray)
        {
            Bounds bounds = new Bounds(WorldPosition, Vector3.one * handleScale);

            float distance = -1;
            bounds.IntersectRay(ray, out distance);
            return distance;
        }
        
        public string ToRichString()
        {
            StringBuilder build = new StringBuilder();
            
            build.Append("X: " + TileCoords.x + " Y: " + TileCoords.y).AppendLine();
            
            build.Append("<mspace=0.5em>");

            int floor = _cameraCoordinator.Current.Floor;
            Map map = _gameManager.Map;
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
                        int heightDifference = TileHeightOrDefault(map[TileCoords.x + i2, TileCoords.y + i], centralHeight, floor) - centralHeight;
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
            return tile?.GetHeightForFloor(floor) ?? defaultHeight;
        }
    }
}
