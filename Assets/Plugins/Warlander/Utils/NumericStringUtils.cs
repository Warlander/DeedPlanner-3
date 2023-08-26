using UnityEngine;

namespace Plugins.Warlander.Utils
{
    public static class NumericStringUtils
    {
        public static int CalculateDigitsCount(int number)
        {
            return number.ToString().Length;
        }
        
        public static string PadIntFromBothSides(int number, int expectedLength)
        {
            int digits = CalculateDigitsCount(number);
            int digitsDiff = expectedLength - digits;
            int digitsDiffHalf = digitsDiff / 2;

            return Spaces(digitsDiffHalf) + number + Spaces(digitsDiff - digitsDiffHalf);
        }

        private static string Spaces(int count)
        {
            return new string(' ', count);
        }
    }
}