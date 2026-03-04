namespace Warlander.Deedplanner.Graphics.Projectors
{
    public interface IToggleableMapProjector : IMapProjector
    {
        void Activate();
        void Deactivate();
    }
}
