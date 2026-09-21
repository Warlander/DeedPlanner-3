namespace Warlander.Deedplanner.Domain
{
    public class MapHeightTracker
    {
        private readonly Map _map;

        private int _lowestSurfaceHeight;
        private int _highestSurfaceHeight;
        private int _lowestCaveHeight;
        private int _highestCaveHeight;
        private bool _boundsDirty;

        public int LowestSurfaceHeight {
            get {
                RecalculateIfDirty();
                return _lowestSurfaceHeight;
            }
        }

        public int HighestSurfaceHeight {
            get {
                RecalculateIfDirty();
                return _highestSurfaceHeight;
            }
        }

        public int LowestCaveHeight {
            get {
                RecalculateIfDirty();
                return _lowestCaveHeight;
            }
        }

        public int HighestCaveHeight {
            get {
                RecalculateIfDirty();
                return _highestCaveHeight;
            }
        }

        public MapHeightTracker(Map map)
        {
            _map = map;
            _boundsDirty = true;
        }

        public void RecalculateHeights()
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            int caveMin = int.MaxValue;
            int caveMax = int.MinValue;

            for (int i = 0; i <= _map.Width; i++)
            {
                for (int i2 = 0; i2 <= _map.Height; i2++)
                {
                    int elevation = _map[i, i2].SurfaceHeight;
                    int caveElevation = _map[i, i2].CaveHeight;
                    if (elevation > max) max = elevation;
                    if (elevation < min) min = elevation;
                    if (caveElevation > caveMax) caveMax = caveElevation;
                    if (caveElevation < caveMin) caveMin = caveElevation;
                }
            }

            _lowestSurfaceHeight = min;
            _highestSurfaceHeight = max;
            _lowestCaveHeight = caveMin;
            _highestCaveHeight = caveMax;
            _boundsDirty = false;
        }

        private void RecalculateIfDirty()
        {
            if (_boundsDirty)
            {
                RecalculateHeights();
            }
        }

        public void RecalculateSurfaceHeight(int x, int y, int previousElevation)
        {
            int elevation = _map[x, y].SurfaceHeight;
            if (elevation > _highestSurfaceHeight) _highestSurfaceHeight = elevation;
            if (elevation < _lowestSurfaceHeight) _lowestSurfaceHeight = elevation;
            if (previousElevation == _highestSurfaceHeight && elevation < previousElevation ||
                previousElevation == _lowestSurfaceHeight && elevation > previousElevation)
            {
                _boundsDirty = true;
            }
            _map.SurfaceGridMesh.SetHeight(x, y, elevation);
        }

        public void RecalculateCaveHeight(int x, int y, int previousElevation)
        {
            int caveElevation = _map[x, y].CaveHeight;
            if (caveElevation > _highestCaveHeight) _highestCaveHeight = caveElevation;
            if (caveElevation < _lowestCaveHeight) _lowestCaveHeight = caveElevation;
            if (previousElevation == _highestCaveHeight && caveElevation < previousElevation ||
                previousElevation == _lowestCaveHeight && caveElevation > previousElevation)
            {
                _boundsDirty = true;
            }
            _map.CaveGridMesh.SetHeight(x, y, caveElevation);
        }
    }
}
