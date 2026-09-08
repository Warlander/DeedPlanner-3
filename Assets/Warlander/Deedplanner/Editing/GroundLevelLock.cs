using System;
using Warlander.Deedplanner.Cameras;
using VContainer.Unity;

namespace Warlander.Deedplanner.Editing
{
    /// <summary>
    /// Restricts editing tabs to the levels their data supports and restores the previous level afterward.
    /// </summary>
    public class GroundLevelLock : IInitializable, IDisposable
    {
        private readonly TabContext _tabContext;
        private readonly CameraCoordinator _cameraCoordinator;

        private bool _locked;
        private MultiCamera _restrictedCamera;
        private int _levelBeforeLock;
        private Tab _lockTab;
        private bool _changingLevel;

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

            return level == 0 || level == -1;
        }

        void IInitializable.Initialize()
        {
            // Camera controllers are not injected yet, so record the starting level without changing it.
            _locked = IsLockTab(_tabContext.CurrentTab);
            _lockTab = _tabContext.CurrentTab;
            if (_locked)
            {
                RestrictCurrentCamera();
            }
            _tabContext.TabChanged += OnTabChanged;
            _cameraCoordinator.CurrentCameraChanged += OnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged += OnLevelChanged;
        }

        void IDisposable.Dispose()
        {
            _tabContext.TabChanged -= OnTabChanged;
            _cameraCoordinator.CurrentCameraChanged -= OnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged -= OnLevelChanged;
        }

        private void OnTabChanged(Tab tab)
        {
            bool shouldLock = IsLockTab(tab);
            bool wasLocked = _locked;
            Tab previousLockTab = _lockTab;

            if (!wasLocked && shouldLock)
            {
                RestrictCurrentCamera();
            }

            _locked = shouldLock;
            _lockTab = tab;
            if (_locked)
            {
                NormalizeCurrentLevel();
            }
            else if (wasLocked)
            {
                int levelToRestore = _levelBeforeLock;
                if (tab == Tab.Caves && levelToRestore >= 0)
                {
                    levelToRestore = -1;
                }
                SetLevel(_restrictedCamera, levelToRestore);
                _restrictedCamera = null;
            }

            if (wasLocked != _locked || previousLockTab != _lockTab)
            {
                LockChanged?.Invoke();
            }
        }

        private static bool IsLockTab(Tab tab)
        {
            return tab == Tab.Ground || tab == Tab.Caves || tab == Tab.Height;
        }

        private void OnCurrentCameraChanged()
        {
            if (_locked)
            {
                MultiCamera previousCamera = _restrictedCamera;
                int previousLevel = _levelBeforeLock;
                RestrictCurrentCamera();
                if (previousCamera != null && previousCamera != _restrictedCamera)
                {
                    SetLevel(previousCamera, previousLevel);
                }
            }
        }

        private void OnLevelChanged()
        {
            if (!_locked || _changingLevel || _cameraCoordinator.Current != _restrictedCamera)
            {
                return;
            }

            if (IsLevelAllowed(_restrictedCamera.Level))
            {
                _levelBeforeLock = _restrictedCamera.Level;
            }
            else
            {
                NormalizeCurrentLevel();
            }
        }

        private void RestrictCurrentCamera()
        {
            _restrictedCamera = _cameraCoordinator.Current;
            _levelBeforeLock = _restrictedCamera.Level;
        }

        private void NormalizeCurrentLevel()
        {
            int level = _cameraCoordinator.Current.Level;
            if (_lockTab == Tab.Ground && level == 0
                || _lockTab == Tab.Caves && level == -1
                || _lockTab == Tab.Height && IsLevelAllowed(level))
            {
                return;
            }

            int normalizedLevel = _lockTab == Tab.Caves || (_lockTab == Tab.Height && level < 0) ? -1 : 0;
            SetLevel(_cameraCoordinator.Current, normalizedLevel);
        }

        private void SetLevel(MultiCamera camera, int level)
        {
            _changingLevel = true;
            try
            {
                camera.Level = level;
            }
            finally
            {
                _changingLevel = false;
            }
        }
    }
}
