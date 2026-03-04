using System;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public interface IMapProjectorCoordinator
    {
        IMapProjector RequestProjector(ProjectorColor color);
        void FreeProjector(IMapProjector projector);
        void Dispose();
    }
}
