using UnityEngine;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class MapRoofCalculator : ILateTickable
    {
        private Map _currentMap;
        private bool _needsRoofUpdate;

        public void SetCurrentMap(Map map)
        {
            _currentMap = map;
        }

        public void ClearCurrentMap()
        {
            _currentMap = null;
        }

        public void ScheduleRecalculation()
        {
            _needsRoofUpdate = true;
        }

        public void LateTick()
        {
            if (!_needsRoofUpdate || _currentMap == null) return;
            _needsRoofUpdate = false;
            RecalculateRoofsInternal();
        }

        private void RecalculateRoofsInternal()
        {
            for (int i = 0; i <= _currentMap.Width; i++)
            {
                for (int i2 = 0; i2 <= _currentMap.Height; i2++)
                {
                    for (int i3 = 0; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = _currentMap[i, i2].GetTileContent(i3);
                        if (entity && entity.GetType() == typeof(Roof))
                            ((Roof)_currentMap[i, i2].GetTileContent(i3)).RecalculateRoofLevel();
                    }
                }
            }

            for (int i = 0; i <= _currentMap.Width; i++)
            {
                for (int i2 = 0; i2 <= _currentMap.Height; i2++)
                {
                    for (int i3 = 0; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = _currentMap[i, i2].GetTileContent(i3);
                        if (entity && entity.GetType() == typeof(Roof))
                            ((Roof)_currentMap[i, i2].GetTileContent(i3)).RecalculateRoofModel();
                    }
                }
            }
        }
    }
}
