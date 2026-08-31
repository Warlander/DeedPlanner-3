using System;

namespace Warlander.Deedplanner.Rendering.Projectors
{
    public interface IMapProjectorCoordinator
    {
        IMapProjector RequestProjector(ProjectorColor color);
        void FreeProjector(IMapProjector projector);
        void Dispose();
    }
}
