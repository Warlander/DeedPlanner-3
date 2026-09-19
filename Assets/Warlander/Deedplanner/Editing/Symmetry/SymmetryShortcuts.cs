using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer.Unity;
using Warlander.Deedplanner.Inputs;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Editing
{
    public sealed class SymmetryShortcuts : IInitializable, IDisposable
    {
        private readonly DPInput _input;
        private readonly SymmetrySession _session;
        private readonly MirrorUpdater _updater;
        private readonly TabContext _tabContext;
        private readonly WindowCoordinator _windowCoordinator;
        private Tab? _returnTab;

        public SymmetryShortcuts(DPInput input, SymmetrySession session, MirrorUpdater updater,
            TabContext tabContext, WindowCoordinator windowCoordinator)
        {
            _input = input;
            _session = session;
            _updater = updater;
            _tabContext = tabContext;
            _windowCoordinator = windowCoordinator;
        }

        public void Initialize()
        {
            _input.Symmetry.PickVerticalAxis.performed += OnPickVertical;
            _input.Symmetry.PickHorizontalAxis.performed += OnPickHorizontal;
            _input.Symmetry.ToggleVerticalAxis.performed += OnToggleVertical;
            _input.Symmetry.ToggleHorizontalAxis.performed += OnToggleHorizontal;
            _input.Symmetry.ClearSymmetry.performed += OnClear;
            _updater.PickEnded += OnPickEnded;
            _tabContext.TabChanged += OnTabChanged;
        }

        public void Dispose()
        {
            _input.Symmetry.PickVerticalAxis.performed -= OnPickVertical;
            _input.Symmetry.PickHorizontalAxis.performed -= OnPickHorizontal;
            _input.Symmetry.ToggleVerticalAxis.performed -= OnToggleVertical;
            _input.Symmetry.ToggleHorizontalAxis.performed -= OnToggleHorizontal;
            _input.Symmetry.ClearSymmetry.performed -= OnClear;
            _updater.PickEnded -= OnPickEnded;
            _tabContext.TabChanged -= OnTabChanged;
        }

        private void OnPickVertical(InputAction.CallbackContext context)
        {
            if (!CanRun())
            {
                return;
            }
            BeginPick();
            _updater.PickVertical();
        }

        private void OnPickHorizontal(InputAction.CallbackContext context)
        {
            if (!CanRun())
            {
                return;
            }
            BeginPick();
            _updater.PickHorizontal();
        }

        private void OnToggleVertical(InputAction.CallbackContext context)
        {
            if (CanRun())
            {
                _session.ToggleVertical();
            }
        }

        private void OnToggleHorizontal(InputAction.CallbackContext context)
        {
            if (CanRun())
            {
                _session.ToggleHorizontal();
            }
        }

        private void OnClear(InputAction.CallbackContext context)
        {
            if (CanRun())
            {
                _session.Clear();
            }
        }

        private bool CanRun()
        {
            if (_windowCoordinator.BlocksGlobalShortcuts())
            {
                return false;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            {
                return true;
            }

            TMP_InputField tmpInput = eventSystem.currentSelectedGameObject.GetComponentInParent<TMP_InputField>();
            InputField input = eventSystem.currentSelectedGameObject.GetComponentInParent<InputField>();
            return (tmpInput == null || !tmpInput.isFocused) && (input == null || !input.isFocused);
        }

        private void BeginPick()
        {
            if (_tabContext.CurrentTab != Tab.Mirror)
            {
                _returnTab = _tabContext.CurrentTab;
                _tabContext.CurrentTab = Tab.Mirror;
            }
        }

        private void OnPickEnded()
        {
            if (!_returnTab.HasValue)
            {
                return;
            }

            Tab returnTab = _returnTab.Value;
            _returnTab = null;
            _tabContext.CurrentTab = returnTab;
        }

        private void OnTabChanged(Tab tab)
        {
            if (tab != Tab.Mirror)
            {
                _returnTab = null;
            }
        }
    }
}
