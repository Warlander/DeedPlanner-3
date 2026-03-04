using System;
using Zenject;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public class MapProjectorFacade : IMapProjectorFacade, IDisposable
    {
        private readonly IMapProjectorCoordinator _coordinator;

        public MapProjectorFacade(IInstantiator instantiator)
        {
            IMapProjectorPrefabsRetriever retriever = new MapProjectorPrefabsRetriever();
            IMapProjectorPrefabs prefabs = retriever.RetrievePrefabs();
            _coordinator = new MapProjectorCoordinator(prefabs, instantiator);
        }

        public IMapProjector RequestProjector(ProjectorColor color)
        {
            return _coordinator.RequestProjector(color);
        }

        public void FreeProjector(IMapProjector projector)
        {
            _coordinator.FreeProjector(projector);
        }

        public void Dispose()
        {
            _coordinator.Dispose();
        }
    }
}
