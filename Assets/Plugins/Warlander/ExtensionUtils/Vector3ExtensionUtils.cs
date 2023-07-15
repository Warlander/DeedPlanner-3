using UnityEngine;

namespace Warlander.ExtensionUtils
{
    /// <summary>
    /// Some vector extensions for math readability improvement.
    /// </summary>
    public static class Vector3Utils
    {
        public static Vector2 ToXY(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }
        
        public static Vector2 ToXZ(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }
        
        public static Vector2 ToYZ(this Vector3 vec)
        {
            return new Vector2(vec.y, vec.z);
        }
        
        public static Vector3 SetX(this Vector3 vec, float newX)
        {
            return new Vector3(newX, vec.y, vec.z);
        }
        
        public static Vector3 SetY(this Vector3 vec, float newY)
        {
            return new Vector3(vec.x, newY, vec.z);
        }
        
        public static Vector3 SetZ(this Vector3 vec, float newZ)
        {
            return new Vector3(vec.x, vec.y, newZ);
        }

        public static Vector3 AddX(this Vector3 vec, float addX)
        {
            return new Vector3(vec.x + addX, vec.y, vec.z);
        }
        
        public static Vector3 AddY(this Vector3 vec, float addY)
        {
            return new Vector3(vec.x, vec.y + addY, vec.z);
        }
        
        public static Vector3 AddZ(this Vector3 vec, float addZ)
        {
            return new Vector3(vec.x, vec.y, vec.z + addZ);
        }
        
        /// <summary>
        /// Return closest full x, y and z values, up or down depending on which one is closer.
        /// </summary>
        public static Vector3Int RoundToInt(this Vector3 vec)
        {
            return new Vector3Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z));
        }
        
        /// <summary>
        /// Return closest full x, y and z values, rounded down.
        /// </summary>
        public static Vector3Int FloorToInt(this Vector3 vec)
        {
            return new Vector3Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y), Mathf.FloorToInt(vec.z));
        }

        /// <summary>
        /// Return closest full x, y and z values, rounded up.
        /// </summary>
        public static Vector3Int CeilToInt(this Vector3 vec)
        {
            return new Vector3Int(Mathf.CeilToInt(vec.x), Mathf.CeilToInt(vec.y), Mathf.CeilToInt(vec.z));
        }
    }
}