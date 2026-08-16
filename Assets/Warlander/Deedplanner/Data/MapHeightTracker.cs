namespace Warlander.Deedplanner.Data
{
    public class MapHeightTracker
    {
        private Map _currentMap;

        public int LowestSurfaceHeight { get; private set; }
        public int HighestSurfaceHeight { get; private set; }
        public int LowestCaveHeight { get; private set; }
        public int HighestCaveHeight { get; private set; }

        public void SetCurrentMap(Map map)
        {
            _currentMap = map;
            LowestSurfaceHeight = 0;
            HighestSurfaceHeight = 0;
            LowestCaveHeight = 0;
            HighestCaveHeight = 0;
        }

        public void ClearCurrentMap()
        {
            _currentMap = null;
        }

        public void RecalculateHeights()
        {
            if (_currentMap == null) return;

            int min = int.MaxValue;
            int max = int.MinValue;
            int caveMin = int.MaxValue;
            int caveMax = int.MinValue;

            for (int i = 0; i <= _currentMap.Width; i++)
            {
                for (int i2 = 0; i2 <= _currentMap.Height; i2++)
                {
                    int elevation = _currentMap[i, i2].SurfaceHeight;
                    int caveElevation = _currentMap[i, i2].CaveHeight;
                    if (elevation > max) max = elevation;
                    if (elevation < min) min = elevation;
                    if (caveElevation > caveMax) caveMax = caveElevation;
                    if (caveElevation < caveMin) caveMin = caveElevation;
                }
            }

            LowestSurfaceHeight = min;
            HighestSurfaceHeight = max;
            LowestCaveHeight = caveMin;
            HighestCaveHeight = caveMax;
        }

        public void RecalculateSurfaceHeight(int x, int y)
        {
            if (_currentMap == null) return;

            int elevation = _currentMap[x, y].SurfaceHeight;
            if (elevation > HighestSurfaceHeight) HighestSurfaceHeight = elevation;
            if (elevation < LowestSurfaceHeight) LowestSurfaceHeight = elevation;
            _currentMap.SurfaceGridMesh.SetHeight(x, y, elevation);
        }

        public void RecalculateCaveHeight(int x, int y)
        {
            if (_currentMap == null) return;

            int caveElevation = _currentMap[x, y].CaveHeight;
            if (caveElevation > HighestCaveHeight) HighestCaveHeight = caveElevation;
            if (caveElevation < LowestCaveHeight) LowestCaveHeight = caveElevation;
            _currentMap.CaveGridMesh.SetHeight(x, y, caveElevation);
        }
    }
}
