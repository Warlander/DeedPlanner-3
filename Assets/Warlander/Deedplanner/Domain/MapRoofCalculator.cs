using Warlander.Deedplanner.Domain.Entities.Roofs;

namespace Warlander.Deedplanner.Domain
{
    public class MapRoofCalculator
    {
        private readonly Map _map;
        private bool _needsRoofUpdate;

        public MapRoofCalculator(Map map)
        {
            _map = map;
        }

        public void ScheduleRecalculation()
        {
            _needsRoofUpdate = true;
        }

        public void RecalculateIfDirty()
        {
            if (!_needsRoofUpdate) return;
            _needsRoofUpdate = false;
            RecalculateRoofsInternal();
        }

        private void RecalculateRoofsInternal()
        {
            for (int i = 0; i <= _map.Width; i++)
            {
                for (int i2 = 0; i2 <= _map.Height; i2++)
                {
                    for (int i3 = Constants.NegativeLevelLimit; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = _map[i, i2].GetTileContent(i3);
                        if (entity && entity.GetType() == typeof(Roof))
                            ((Roof)_map[i, i2].GetTileContent(i3)).RecalculateRoofLevel();
                    }
                }
            }

            for (int i = 0; i <= _map.Width; i++)
            {
                for (int i2 = 0; i2 <= _map.Height; i2++)
                {
                    for (int i3 = Constants.NegativeLevelLimit; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = _map[i, i2].GetTileContent(i3);
                        if (entity && entity.GetType() == typeof(Roof))
                            ((Roof)_map[i, i2].GetTileContent(i3)).RecalculateRoofModel();
                    }
                }
            }
        }
    }
}
