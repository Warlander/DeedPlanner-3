using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public class MapProjectorCoordinator : IMapProjectorCoordinator
    {
        private readonly IMapProjectorPrefabs _prefabs;
        private readonly IObjectResolver _resolver;
        private readonly Transform _container;
        private readonly Dictionary<ProjectorColor, Stack<IToggleableMapProjector>> _freeProjectors = new();

        public MapProjectorCoordinator(IMapProjectorPrefabs prefabs, IObjectResolver resolver)
        {
            _prefabs = prefabs;
            _resolver = resolver;
            _container = new GameObject("MapProjectors Pool").transform;
        }

        public IMapProjector RequestProjector(ProjectorColor color)
        {
            EnsureColorStack(color);

            if (_freeProjectors[color].Count > 0)
            {
                IToggleableMapProjector projector = _freeProjectors[color].Pop();
                projector.Activate();
                projector.MarkRenderWithAllCameras();
                return projector;
            }

            return CreateProjector(color);
        }

        public void FreeProjector(IMapProjector projector)
        {
            IToggleableMapProjector toggleable = (IToggleableMapProjector)projector;
            EnsureColorStack(toggleable.Color);
            toggleable.Deactivate();
            _freeProjectors[toggleable.Color].Push(toggleable);
        }

        private IToggleableMapProjector CreateProjector(ProjectorColor color)
        {
            IToggleableMapProjector prefab = _prefabs.GetPrefabForColor(color);
            if (prefab == null)
                return null;

            return _resolver.Instantiate<MapProjector>((MapProjector)prefab, _container);
        }

        private void EnsureColorStack(ProjectorColor color)
        {
            if (!_freeProjectors.ContainsKey(color))
                _freeProjectors[color] = new Stack<IToggleableMapProjector>();
        }

        public void Dispose()
        {
            if (_container != null)
                UnityEngine.Object.Destroy(_container.gameObject);
        }
    }
}
