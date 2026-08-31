namespace Warlander.Deedplanner.Rendering.Projectors
{
    public interface IToggleableMapProjector : IMapProjector
    {
        void Activate();
        void Deactivate();
    }
}
