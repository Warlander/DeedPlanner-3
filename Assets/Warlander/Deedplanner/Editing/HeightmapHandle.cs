using UnityEngine;

namespace Warlander.Deedplanner.Editing
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
        
    }
}
