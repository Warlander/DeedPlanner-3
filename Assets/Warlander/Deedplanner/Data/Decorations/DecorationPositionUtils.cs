using UnityEngine;

namespace Warlander.Deedplanner.Data.Decorations
{
    public static class DecorationPositionUtils
    {

        private const float BottomAlign = 2f/3f;
        private const float MiddleAlign = 2f;
        private const float TopAlign = 10f/3f;

        private const float LeftAlign = 2f/3f;
        private const float CenterAlign = 2f;
        private const float RightAlign = 10f/3f;
        
        public static Vector2 ParseDecorationPositionEnum(string positionString)
        {
            string upperPositionString = positionString.ToUpper();

            switch (upperPositionString)
            {
                case "CORNER":
                    return Vector2.zero;
                case "TOP_LEFT":
                    return new Vector2(LeftAlign, TopAlign);
                case "TOP_CENTER":
                    return new Vector2(CenterAlign, TopAlign);
                case "TOP_RIGHT":
                    return new Vector2(RightAlign, TopAlign);
                case "MIDDLE_LEFT":
                    return new Vector2(LeftAlign, MiddleAlign);
                case "MIDDLE_CENTER":
                    return new Vector2(CenterAlign, MiddleAlign);
                case "MIDDLE_RIGHT":
                    return new Vector2(RightAlign, MiddleAlign);
                case "BOTTOM_LEFT":
                    return new Vector2(LeftAlign, BottomAlign);
                case "BOTTOM_CENTER":
                    return new Vector2(CenterAlign, BottomAlign);
                case "BOTTOM_RIGHT":
                    return new Vector2(RightAlign, BottomAlign);
                default:
                    Debug.LogWarning("Invalid position string: " + upperPositionString);
                    return Vector2.zero;
            }
        }
        
    }
}