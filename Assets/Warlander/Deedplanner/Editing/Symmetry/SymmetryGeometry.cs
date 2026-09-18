using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public static class SymmetryGeometry
    {
        public static int SnapAxisCoordinate2(float worldCoordinate)
        {
            float doubledTileCoordinate = worldCoordinate / 2f;
            int lower = Mathf.FloorToInt(doubledTileCoordinate);
            int upper = Mathf.CeilToInt(doubledTileCoordinate);
            float lowerDistance = doubledTileCoordinate - lower;
            float upperDistance = upper - doubledTileCoordinate;

            if (Mathf.Approximately(lowerDistance, upperDistance))
            {
                return lower % 2 == 0 ? lower : upper;
            }

            return lowerDistance < upperDistance ? lower : upper;
        }
    }
}
