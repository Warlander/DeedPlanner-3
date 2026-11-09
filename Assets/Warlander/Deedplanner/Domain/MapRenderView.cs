using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Domain
{
    public enum ModelPropertyBlockRetention
    {
        Preserve,
        Enabled,
        Disabled
    }

    public readonly struct MapRenderView
    {
        public int Level { get; }
        public bool RenderEntireMap { get; }
        public bool RenderGrid { get; }
        public ModelPropertyBlockRetention PropertyBlockRetention { get; }
        public bool IsUnderground => Level < 0;
        public int StoreyIndex => IsUnderground ? CaveLevel.GetStoreyIndex(Level) : Level;

        public MapRenderView(int level, bool renderEntireMap, bool renderGrid,
            ModelPropertyBlockRetention propertyBlockRetention = ModelPropertyBlockRetention.Preserve)
        {
            Level = level;
            RenderEntireMap = renderEntireMap;
            RenderGrid = renderGrid;
            PropertyBlockRetention = propertyBlockRetention;
        }
    }
}
