using UnityEngine;

namespace Warlander.ExtensionUtils
{
    /// <summary>
    /// Some vector extensions for math readability improvement.
    /// </summary>
    public static class Vector2Utils
    {
        public static Vector3 ToVector3XY(this Vector2 vec, float z = 0)
        {
            return new Vector3(vec.x, vec.y, z);
        }

        public static Vector3 ToVector3XZ(this Vector2 vec, float y = 0)
        {
            return new Vector3(vec.x, y, vec.y);
        }
        
        public static Vector2 SetX(this Vector2 vec, float newX)
        {
            return new Vector2(newX, vec.y);
        }
        
        public static Vector2 SetY(this Vector2 vec, float newY)
        {
            return new Vector2(vec.x, newY);
        }

        public static Vector2 AddX(this Vector2 vec, float addX)
        {
            return new Vector2(vec.x + addX, vec.y);
        }
        
        public static Vector2 AddY(this Vector2 vec, float addY)
        {
            return new Vector2(vec.x, vec.y + addY);
        }

        /// <summary>
        /// Return closest full x and y values, up or down depending on which one is closer.
        /// </summary>
        public static Vector2Int RoundToInt(this Vector2 vec)
        {
            return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
        }
        
        /// <summary>
        /// Return closest full x and y values, rounded down.
        /// </summary>
        public static Vector2Int FloorToInt(this Vector2 vec)
        {
            return new Vector2Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
        }

        /// <summary>
        /// Return closest full x and y values, rounded up.
        /// </summary>
        public static Vector2Int CeilToInt(this Vector2 vec)
        {
            return new Vector2Int(Mathf.CeilToInt(vec.x), Mathf.CeilToInt(vec.y));
        }

        public static float Max(this Vector2 vec)
        {
            return Mathf.Max(vec.x, vec.y);
        }
    }
}