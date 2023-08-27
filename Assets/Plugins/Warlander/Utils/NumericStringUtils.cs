using UnityEngine;

namespace Plugins.Warlander.Utils
{
    public static class NumericStringUtils
    {
        public static int CalculateDigitsCount(int number)
        {
            return number.ToString().Length;
        }

        /// <summary>
        /// TextMeshPro-specific padding method with proper support for half-spaces.
        /// </summary>
        public static string PadIntFromBothSidesTMP(int number, int expectedLength)
        {
            return PadIntFromBothSides(number, expectedLength, "<space=0.25em>");
        }
        
        public static string PadIntFromBothSides(int number, int expectedLength, string halfSpaceString = " ")
        {
            int digits = CalculateDigitsCount(number);
            int digitsDiff = expectedLength - digits;
            int digitsDiffHalf = digitsDiff / 2;
            bool hasLeftoverSpace = digitsDiff % 2 == 1;

            if (hasLeftoverSpace)
            {
                return Spaces(digitsDiffHalf) + halfSpaceString + number + halfSpaceString + Spaces(digitsDiffHalf);
            }
            else
            {
                return Spaces(digitsDiffHalf) + number + Spaces(digitsDiffHalf);
            }
        }

        private static string Spaces(int count)
        {
            return new string(' ', count);
        }
    }
}