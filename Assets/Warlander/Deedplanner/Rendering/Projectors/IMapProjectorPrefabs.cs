namespace Warlander.Deedplanner.Rendering.Projectors
{
    public interface IMapProjectorPrefabs
    {
        IToggleableMapProjector GetPrefabForColor(ProjectorColor color);
    }
}
