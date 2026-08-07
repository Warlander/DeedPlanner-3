using System.Collections.Generic;
using Warlander.Deedplanner.Updaters;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    public class UpdaterCoordinator : IInitializable, ITickable
    {
        private readonly IReadOnlyList<IUpdater> _updaters;
        private readonly TabContext _tabContext;
        private readonly MapHandler _mapHandler;

        private IUpdater _currentUpdater;

        public UpdaterCoordinator(IReadOnlyList<IUpdater> updaters, TabContext tabContext, MapHandler mapHandler)
        {
            _updaters = updaters;
            _tabContext = tabContext;
            _mapHandler = mapHandler;
        }

        void IInitializable.Initialize()
        {
            foreach (IUpdater updater in _updaters)
            {
                updater.Initialize();
            }

            _tabContext.TabChanged += OnTabChange;
            OnTabChange(_tabContext.CurrentTab);
        }

        void ITickable.Tick()
        {
            _currentUpdater?.Tick();
        }

        private void OnTabChange(Tab tab)
        {
            _currentUpdater?.Disable();

            _currentUpdater = null;
            foreach (IUpdater updater in _updaters)
            {
                if (updater.TargetTab == tab)
                {
                    _currentUpdater = updater;
                    break;
                }
            }

            _currentUpdater?.Enable();

            if (_mapHandler.Map != null)
            {
                _mapHandler.Map.RenderGrid = tab != Tab.Menu;
            }
        }
    }
}
