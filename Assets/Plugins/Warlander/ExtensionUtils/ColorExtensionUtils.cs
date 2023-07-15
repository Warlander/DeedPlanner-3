using UnityEngine;

namespace Warlander.ExtensionUtils
{
    public static class ColorExtensionUtils
    {
        public static Color SetR(this Color color, float newRed)
        {
            return new Color(newRed, color.g, color.b, color.a);
        }
        
        public static Color SetG(this Color color, float newGreen)
        {
            return new Color(color.r, newGreen, color.b, color.a);
        }

        public static Color SetBlue(this Color color, float newBlue)
        {
            return new Color(color.r, color.g, newBlue, color.a);
        }
        
        public static Color SetA(this Color color, float newAlpha)
        {
            return new Color(color.r, color.g, color.b, newAlpha);
        }
    }
}