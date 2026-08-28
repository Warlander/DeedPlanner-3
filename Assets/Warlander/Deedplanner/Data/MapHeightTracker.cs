namespace Warlander.Deedplanner.Data
{
    public class MapHeightTracker
    {
        private Map _currentMap;

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

        public void SetCurrentMap(Map map)
        {
            _currentMap = map;
            _lowestSurfaceHeight = 0;
            _highestSurfaceHeight = 0;
            _lowestCaveHeight = 0;
            _highestCaveHeight = 0;
            _boundsDirty = true;
        }

        // destroyed maps call this from OnDestroy, which runs end-of-frame - potentially AFTER
        // the replacement map already registered itself; only clear if it is still the owner
        public void ClearCurrentMap(Map map)
        {
            if (_currentMap == map)
            {
                _currentMap = null;
            }
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
            if (_currentMap == null) return;

            int elevation = _currentMap[x, y].SurfaceHeight;
            if (elevation > _highestSurfaceHeight) _highestSurfaceHeight = elevation;
            if (elevation < _lowestSurfaceHeight) _lowestSurfaceHeight = elevation;
            if (previousElevation == _highestSurfaceHeight && elevation < previousElevation ||
                previousElevation == _lowestSurfaceHeight && elevation > previousElevation)
            {
                _boundsDirty = true;
            }
            _currentMap.SurfaceGridMesh.SetHeight(x, y, elevation);
        }

        public void RecalculateCaveHeight(int x, int y, int previousElevation)
        {
            if (_currentMap == null) return;

            int caveElevation = _currentMap[x, y].CaveHeight;
            if (caveElevation > _highestCaveHeight) _highestCaveHeight = caveElevation;
            if (caveElevation < _lowestCaveHeight) _lowestCaveHeight = caveElevation;
            if (previousElevation == _highestCaveHeight && caveElevation < previousElevation ||
                previousElevation == _lowestCaveHeight && caveElevation > previousElevation)
            {
                _boundsDirty = true;
            }
            _currentMap.CaveGridMesh.SetHeight(x, y, caveElevation);
        }
    }
}
