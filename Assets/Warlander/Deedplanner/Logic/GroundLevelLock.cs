using System;
using Warlander.Deedplanner.Cameras;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    /// <summary>
    /// Locks the viewed level to the ground-level set while a surface-only editing tab
    /// (Ground, Height) is active. Restores the previous level when the lock ends.
    /// </summary>
    public class GroundLevelLock : IInitializable, IDisposable
    {
        private static readonly int[] AllowedLevels = { 0 };

        private readonly TabContext _tabContext;
        private readonly CameraCoordinator _cameraCoordinator;

        private bool _locked;
        private int _levelBeforeLock;

        public event Action LockChanged;

        public bool Locked => _locked;

        public GroundLevelLock(TabContext tabContext, CameraCoordinator cameraCoordinator)
        {
            _tabContext = tabContext;
            _cameraCoordinator = cameraCoordinator;
        }

        public bool IsLevelAllowed(int level)
        {
            if (!_locked)
            {
                return true;
            }

            return Array.IndexOf(AllowedLevels, level) >= 0;
        }

        void IInitializable.Initialize()
        {
            // Camera controllers are not injected yet during container build,
            // so only adopt the lock state here; the camera starts at level 0 anyway.
            _locked = IsLockTab(_tabContext.CurrentTab);
            _tabContext.TabChanged += OnTabChanged;
            _cameraCoordinator.CurrentCameraChanged += OnCurrentCameraChanged;
        }

        void IDisposable.Dispose()
        {
            _tabContext.TabChanged -= OnTabChanged;
            _cameraCoordinator.CurrentCameraChanged -= OnCurrentCameraChanged;
        }

        private void OnTabChanged(Tab tab)
        {
            bool shouldLock = IsLockTab(tab);
            if (shouldLock == _locked)
            {
                return;
            }

            _locked = shouldLock;
            if (_locked)
            {
                _levelBeforeLock = _cameraCoordinator.Current.Level;
                _cameraCoordinator.Current.Level = AllowedLevels[0];
            }
            else
            {
                int levelToRestore = _levelBeforeLock;
                if (tab == Tab.Caves && levelToRestore >= 0)
                {
                    levelToRestore = -1;
                }
                _cameraCoordinator.Current.Level = levelToRestore;
            }

            LockChanged?.Invoke();
        }

        private static bool IsLockTab(Tab tab)
        {
            return tab == Tab.Ground || tab == Tab.Height;
        }

        private void OnCurrentCameraChanged()
        {
            if (_locked && !IsLevelAllowed(_cameraCoordinator.Current.Level))
            {
                _cameraCoordinator.Current.Level = AllowedLevels[0];
            }
        }
    }
}
