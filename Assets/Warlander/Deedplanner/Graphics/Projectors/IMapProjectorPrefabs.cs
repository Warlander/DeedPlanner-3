namespace Warlander.Deedplanner.Graphics.Projectors
{
    public interface IMapProjectorPrefabs
    {
        IToggleableMapProjector GetPrefabForColor(ProjectorColor color);
    }
}
