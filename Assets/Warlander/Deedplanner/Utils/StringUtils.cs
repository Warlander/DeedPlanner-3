namespace Warlander.Deedplanner.Utils
{
    public static class StringUtils
    {
        
        public static int DigitsStringCount(int number)
        {
            return number.ToString().Length;
        }
        
        public static string PaddedNumberString(int number, int maxDigits)
        {
            int digits = DigitsStringCount(number);
            int digitsDiff = maxDigits - digits;
            int digitsDiffHalf = digitsDiff / 2;

            return Spaces(digitsDiffHalf) + number + Spaces(digitsDiff - digitsDiffHalf);
        }

        private static string Spaces(int count)
        {
            return new string(' ', count);
        }
        
    }
}