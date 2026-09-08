using System;

namespace Warlander.Deedplanner.Caves
{
    public static class CaveLevel
    {
        public const int HeightUnitsPerStorey = 30;
        public const float WorldUnitsPerStorey = 3f;

        public static int GetStoreyIndex(int level)
        {
            if (level >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "A cave level must be negative.");
            }

            return -level - 1;
        }

        public static int GetHeightOffset(int level)
        {
            return GetStoreyIndex(level) * HeightUnitsPerStorey;
        }

        public static float GetWorldHeightOffset(int level)
        {
            return GetStoreyIndex(level) * WorldUnitsPerStorey;
        }

        public static int GetAbsoluteHeight(int floorHeight, int level)
        {
            return floorHeight + GetHeightOffset(level);
        }

        public static float GetWorldHeight(int floorHeight, int level)
        {
            return floorHeight * 0.1f + GetWorldHeightOffset(level);
        }
    }
}
