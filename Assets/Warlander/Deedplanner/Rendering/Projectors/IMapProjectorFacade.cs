namespace Warlander.Deedplanner.Rendering.Projectors
{
    public interface IMapProjectorFacade
    {
        IMapProjector RequestProjector(ProjectorColor color);
        void FreeProjector(IMapProjector projector);
    }
}
