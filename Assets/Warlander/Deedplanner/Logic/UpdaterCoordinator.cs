using Warlander.Deedplanner.Updaters;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class UpdaterCoordinator : IInitializable, ITickable
    {
        private readonly AbstractUpdater[] _updaters;
        private readonly TabContext _tabContext;
        private readonly MapHandler _mapHandler;

        private AbstractUpdater _currentUpdater;

        public UpdaterCoordinator(AbstractUpdater[] updaters, TabContext tabContext, MapHandler mapHandler)
        {
            _updaters = updaters;
            _tabContext = tabContext;
            _mapHandler = mapHandler;
        }

        void IInitializable.Initialize()
        {
            foreach (AbstractUpdater updater in _updaters)
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
            foreach (AbstractUpdater updater in _updaters)
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
