using System.Collections.Generic;
using Warlander.Deedplanner.Updaters;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    public class UpdaterCoordinator : IInitializable, ITickable
    {
        private readonly IReadOnlyList<AbstractUpdater> _updaters;
        private readonly TabContext _tabContext;
        private readonly MapHandler _mapHandler;

        private AbstractUpdater _currentUpdater;

        public UpdaterCoordinator(IReadOnlyList<AbstractUpdater> updaters, TabContext tabContext, MapHandler mapHandler)
        {
            _updaters = updaters;
            _tabContext = tabContext;
            _mapHandler = mapHandler;
        }

        void IInitializable.Initialize()
        {
            foreach (AbstractUpdater updater in _updaters)
            {
                updater.gameObject.SetActive(false);
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
            if (_currentUpdater != null)
                _currentUpdater.gameObject.SetActive(false);

            _currentUpdater = null;
            foreach (AbstractUpdater updater in _updaters)
            {
                if (updater.TargetTab == tab)
                {
                    _currentUpdater = updater;
                    break;
                }
            }

            if (_currentUpdater != null)
                _currentUpdater.gameObject.SetActive(true);
            _currentUpdater?.Enable();

            if (_mapHandler.Map != null)
            {
                _mapHandler.Map.RenderGrid = tab != Tab.Menu;
            }
        }
    }
}
