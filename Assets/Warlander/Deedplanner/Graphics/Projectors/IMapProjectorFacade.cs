namespace Warlander.Deedplanner.Graphics.Projectors
{
    public interface IMapProjectorFacade
    {
        IMapProjector RequestProjector(ProjectorColor color);
        void FreeProjector(IMapProjector projector);
    }
}
