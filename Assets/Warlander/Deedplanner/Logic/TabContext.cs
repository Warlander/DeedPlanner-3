using System;
using Warlander.Deedplanner.Logic.Cameras;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    public class TabContext : IInitializable, IDisposable
    {
        private readonly CameraCoordinator _cameraCoordinator;

        private Tab _currentTab;

        public event Action<Tab> TabChanged;

        public TileSelectionMode TileSelectionMode { get; set; }

        public Tab CurrentTab
        {
            get => _currentTab;
            set
            {
                _currentTab = value;
                TabChanged?.Invoke(value);
            }
        }

        public TabContext(CameraCoordinator cameraCoordinator)
        {
            _cameraCoordinator = cameraCoordinator;
        }

        void IInitializable.Initialize()
        {
            _cameraCoordinator.LevelChanged += OnLevelChanged;
        }

        void IDisposable.Dispose()
        {
            _cameraCoordinator.LevelChanged -= OnLevelChanged;
        }

        private void OnLevelChanged()
        {
            int level = _cameraCoordinator.Current.Level;
            if (level < 0 && _currentTab == Tab.Ground)
                CurrentTab = Tab.Caves;
            else if (level >= 0 && _currentTab == Tab.Caves)
                CurrentTab = Tab.Ground;
        }
    }
}
