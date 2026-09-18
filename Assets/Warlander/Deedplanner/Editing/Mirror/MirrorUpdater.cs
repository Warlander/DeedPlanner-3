using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Rendering.Projectors;
using System;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Windows;
using Warlander.Deedplanner.Ui;
using Warlander.UI.Windows;
using Warlogic.Settings;
using UnityEngine.InputSystem;
using DeedPlannerInputSettings = Warlander.Deedplanner.Settings.InputSettings;

namespace Warlander.Deedplanner.Editing
{
    public class MirrorUpdater : IUpdater
    {
        private enum PickMode
        {
            None,
            Vertical,
            Horizontal
        }

        private readonly IMirrorUpdaterView _view;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly TabContext _tabContext;
        private readonly SymmetrySession _session;
        private readonly IMapProjectorFacade _mapProjectorFacade;
        private readonly Inputs.DPInput _input;
        private readonly SettingsRegistry _settingsRegistry;
        private readonly DeedPlannerInputSettings _inputSettings;
        private readonly WindowCoordinator _windowCoordinator;
        private string _shortcutLabels;

        private PickMode _pickMode;
        private IMapProjector _verticalProjector;
        private IMapProjector _horizontalProjector;
        private IMapProjector _previewProjector;

        public event Action PickEnded = delegate { };

        public Tab TargetTab => Tab.Mirror;

        public MirrorUpdater(IMirrorUpdaterView view, CameraCoordinator cameraCoordinator, TabContext tabContext,
            SymmetrySession session, IMapProjectorFacade mapProjectorFacade, Inputs.DPInput input,
            SettingsRegistry settingsRegistry, DeedPlannerInputSettings inputSettings,
            WindowCoordinator windowCoordinator)
        {
            _view = view;
            _cameraCoordinator = cameraCoordinator;
            _tabContext = tabContext;
            _session = session;
            _mapProjectorFacade = mapProjectorFacade;
            _input = input;
            _settingsRegistry = settingsRegistry;
            _inputSettings = inputSettings;
            _windowCoordinator = windowCoordinator;
        }

        public void Initialize()
        {
            _view.PickVerticalClicked += PickVertical;
            _view.PickHorizontalClicked += PickHorizontal;
            _view.VerticalEnabledChanged += value => _session.SetVerticalEnabled(value);
            _view.HorizontalEnabledChanged += value => _session.SetHorizontalEnabled(value);
            _view.ClearClicked += _session.Clear;
            _view.ConfigureShortcutsClicked += OpenSettings;
            _session.Changed += Refresh;
            Refresh();
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Nothing;
            RefreshProjectors();
            RefreshShortcutLabels();
        }

        public void Disable()
        {
            SetPickMode(PickMode.None);
        }

        public void Tick()
        {
            RefreshShortcutLabels();
            if (_input.UpdatersShared.Deletion.WasPressedThisFrame()
                || _input.UI.Cancel.WasPressedThisFrame())
            {
                SetPickMode(PickMode.None);
                return;
            }

            if (_pickMode == PickMode.None)
            {
                return;
            }

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            int coordinate2 = _pickMode == PickMode.Vertical
                ? SymmetryGeometry.SnapAxisCoordinate2(raycast.point.x)
                : SymmetryGeometry.SnapAxisCoordinate2(raycast.point.z);
            _previewProjector.ProjectAxisLine(coordinate2, _pickMode == PickMode.Vertical
                ? PlaneAlignment.Vertical
                : PlaneAlignment.Horizontal);

            if (!_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                return;
            }

            if (_pickMode == PickMode.Vertical)
            {
                _session.PlaceVerticalAxis(coordinate2);
            }
            else
            {
                _session.PlaceHorizontalAxis(coordinate2);
            }
            SetPickMode(PickMode.None);
        }

        public void PickVertical() => SetPickMode(PickMode.Vertical);
        public void PickHorizontal() => SetPickMode(PickMode.Horizontal);

        private void SetPickMode(PickMode mode)
        {
            bool ending = _pickMode != PickMode.None && mode == PickMode.None;
            bool changing = _pickMode != mode;
            _pickMode = mode;
            if (changing && _previewProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_previewProjector);
                _previewProjector = null;
            }
            if (mode != PickMode.None && _previewProjector == null)
            {
                _previewProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Green);
            }
            _view.SetPickMode(mode == PickMode.Vertical, mode == PickMode.Horizontal);
            if (ending)
            {
                PickEnded();
            }
        }

        private void Refresh()
        {
            bool hasVertical = _session.TryGetVerticalAxis(out int verticalCoordinate2);
            bool hasHorizontal = _session.TryGetHorizontalAxis(out int horizontalCoordinate2);
            _view.SetState(hasVertical, _session.IsVerticalEnabled, FormatPosition("X", verticalCoordinate2),
                hasHorizontal, _session.IsHorizontalEnabled, FormatPosition("Y", horizontalCoordinate2));
            RefreshProjectors();
        }

        private void RefreshProjectors()
        {
            ReleaseProjectors();
            if (_session.TryGetVerticalAxis(out int verticalCoordinate2))
            {
                _verticalProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                _verticalProjector.ProjectAxisLine(verticalCoordinate2, PlaneAlignment.Vertical);
            }
            if (_session.TryGetHorizontalAxis(out int horizontalCoordinate2))
            {
                _horizontalProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                _horizontalProjector.ProjectAxisLine(horizontalCoordinate2, PlaneAlignment.Horizontal);
            }
        }

        private void ReleaseProjectors()
        {
            if (_verticalProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_verticalProjector);
                _verticalProjector = null;
            }
            if (_horizontalProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_horizontalProjector);
                _horizontalProjector = null;
            }
        }

        private static string FormatPosition(string axis, int coordinate2)
        {
            return axis + " = " + (coordinate2 * 0.5f).ToString("0.#");
        }

        private void RefreshShortcutLabels()
        {
            string labels = "Pick vertical — " + Display(_input.Symmetry.PickVerticalAxis) + "\n"
                + "Pick horizontal — " + Display(_input.Symmetry.PickHorizontalAxis) + "\n"
                + "Toggle vertical — " + Display(_input.Symmetry.ToggleVerticalAxis) + "\n"
                + "Toggle horizontal — " + Display(_input.Symmetry.ToggleHorizontalAxis) + "\n"
                + "Clear symmetry — " + Display(_input.Symmetry.ClearSymmetry);
            if (labels == _shortcutLabels)
            {
                return;
            }
            _shortcutLabels = labels;
            _view.SetShortcutLabels(labels);
        }

        private static string Display(InputAction action)
        {
            string display = action.GetBindingDisplayString();
            return string.IsNullOrEmpty(display) ? "Unbound" : display;
        }

        private void OpenSettings()
        {
            Window window = _windowCoordinator.CreateWindowExclusive(WindowNames.SettingsWindow);
            if (window == null)
            {
                return;
            }
            var view = window.GetComponent<SettingsWindowView>();
            _ = new SettingsWindowSession(_settingsRegistry, _inputSettings, view);
        }
    }
}
