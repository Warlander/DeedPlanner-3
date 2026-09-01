using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Domain
{
    public class MapRoofCalculator : ILateTickable
    {
        private Map _currentMap;
        private bool _needsRoofUpdate;

        public void SetCurrentMap(Map map)
        {
            _currentMap = map;
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
