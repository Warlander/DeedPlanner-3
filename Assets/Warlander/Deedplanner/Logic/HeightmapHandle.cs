using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public class HeightmapHandle
    {
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
                propertyBlock.SetColor(ShaderPropertyIds.BaseColor, value);
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
        
        public void WriteSlopeGridData(Map map, int floor, int[] heightsBuffer)
        {
            Tile centralTile = map[TileCoords.x, TileCoords.y];
            int centralHeight = centralTile.GetHeightForLevel(floor);

            int index = 0;
            for (int i = 1; i >= -1; i--)
            {
                for (int i2 = -1; i2 <= 1; i2++)
                {
                    heightsBuffer[index++] = TileHeightOrDefault(map[TileCoords.x + i2, TileCoords.y + i], centralHeight, floor);
                }
            }
        }
        
        private static int TileHeightOrDefault(Tile tile, int defaultHeight, int floor)
        {
            return tile?.GetHeightForLevel(floor) ?? defaultHeight;
        }
    }
}
