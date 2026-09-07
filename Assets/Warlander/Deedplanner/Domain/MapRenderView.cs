using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Domain
{
    public readonly struct MapRenderView
    {
        public int Level { get; }
        public bool RenderEntireMap { get; }
        public bool RenderGrid { get; }
        public bool IsUnderground => Level < 0;
        public int StoreyIndex => IsUnderground ? CaveLevel.GetStoreyIndex(Level) : Level;

        public MapRenderView(int level, bool renderEntireMap, bool renderGrid)
        {
            Level = level;
            RenderEntireMap = renderEntireMap;
            RenderGrid = renderGrid;
        }
    }
}
