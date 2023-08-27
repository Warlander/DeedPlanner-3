using UnityEngine;

namespace Warlander.ExtensionUtils
{
    public static class RectExtensionUtils
    {
        public static Rect ShiftPosition(this Rect rect, Vector2 shift)
        {
            return new Rect(rect.position + shift, rect.size);
        }

        /// <summary>
        /// Origin is (0, 0).
        /// </summary>
        public static Rect ShiftToOrigin(this Rect rect)
        {
            return rect.ShiftPosition(-rect.position);
        }
    }
}