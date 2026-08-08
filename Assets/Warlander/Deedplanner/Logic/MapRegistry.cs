using System;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public class MapRegistry
    {
        public Map CurrentMap { get; private set; }

        public event Action MapInitialized;

        public void SetMap(Map map)
        {
            CurrentMap = map;
            MapInitialized?.Invoke();
        }
    }
}
