using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public class FloorUpdaterView : MonoBehaviour, IFloorUpdaterView
    {
        // Toggles in _stoneVariantToggles must be in the same order as these catalog shortnames.
        private static readonly string[] StoneVariantShortNames = { "dsp", "drsp", "dslp", "dmp", "dssp", "dpbp" };

        [SerializeField] private UnityTree _floorsTree;
        [SerializeField] private UnityList _dockFloorsList;

        [SerializeField] private Toggle _southToggle;
        [SerializeField] private Toggle _westToggle;
        [SerializeField] private Toggle _northToggle;
        [SerializeField] private Toggle _eastToggle;

        [SerializeField] private Toggle _floorsModeToggle;
        [SerializeField] private Toggle _docksModeToggle;

        [SerializeField] private GameObject _dockSupportSection;
        [SerializeField] private Toggle _autoSupportToggle;
        [SerializeField] private Toggle _noneSupportToggle;
        [SerializeField] private Toggle _woodSupportToggle;
        [SerializeField] private Toggle _stoneSupportToggle;
        [SerializeField] private Toggle _braceSupportToggle;
        [SerializeField] private GameObject _stoneVariantRow;
        [SerializeField] private Toggle[] _stoneVariantToggles;

        public event Action<FloorData> FloorSelected;
        public event Action<EntityOrientation> OrientationChanged;
        public event Action<FloorPaintMode> PaintModeChanged;
        public event Action<bool, DockSupportData> DockSupportChanged;

        private void Awake()
        {
            _floorsTree.ValueChanged += OnFloorsTreeValueChanged;
            _dockFloorsList.ValueChanged += OnDockFloorsListValueChanged;
            _southToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Down));
            _westToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Right));
            _northToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Up));
            _eastToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Left));

            _floorsModeToggle.onValueChanged.AddListener(toggled => OnPaintModeToggled(toggled, FloorPaintMode.Floors));
            _docksModeToggle.onValueChanged.AddListener(toggled => OnPaintModeToggled(toggled, FloorPaintMode.Docks));

            _autoSupportToggle.onValueChanged.AddListener(toggled => OnDockSupportToggled(toggled, true, null));
            _noneSupportToggle.onValueChanged.AddListener(toggled => OnDockSupportToggled(toggled, false, null));
            _woodSupportToggle.onValueChanged.AddListener(toggled => OnDockSupportToggled(toggled, false, Database.DockSupports["dwp"]));
            _stoneSupportToggle.onValueChanged.AddListener(toggled => OnStoneSupportToggled(toggled));
            _braceSupportToggle.onValueChanged.AddListener(toggled => OnDockSupportToggled(toggled, false, Database.DockSupports["dwb"]));

            for (int i = 0; i < _stoneVariantToggles.Length; i++)
            {
                DockSupportData variant = Database.DockSupports[StoneVariantShortNames[i]];
                _stoneVariantToggles[i].onValueChanged.AddListener(toggled => OnDockSupportToggled(toggled, false, variant));
            }

            // Visibility always derives from toggle state, never from serialized active flags.
            _dockSupportSection.SetActive(_docksModeToggle.isOn);
            _stoneVariantRow.SetActive(_stoneSupportToggle.isOn);
            UpdateFloorPickerVisibility(_docksModeToggle.isOn);
        }

        public void AddFloorEntry(FloorData data, string[] category)
        {
            _floorsTree.Add(data, category);
        }

        public void AddDockFloorEntry(FloorData data)
        {
            _dockFloorsList.Add(data);
        }

        public void SetDockSupportSectionVisible(bool visible)
        {
            _dockSupportSection.SetActive(visible);
        }

        public void PushSelection()
        {
            if (_docksModeToggle.isOn)
            {
                OnDockFloorsListValueChanged(_dockFloorsList.SelectedValue);
            }
            else
            {
                OnFloorsTreeValueChanged(_floorsTree.SelectedValue);
            }
        }

        private void OnFloorsTreeValueChanged(object value)
        {
            FloorSelected?.Invoke(value as FloorData);
        }

        private void OnDockFloorsListValueChanged(object value)
        {
            FloorSelected?.Invoke(value as FloorData);
        }

        private void OnOrientationToggled(bool toggledOn, EntityOrientation orientation)
        {
            if (toggledOn)
            {
                OrientationChanged?.Invoke(orientation);
            }
        }

        private void OnPaintModeToggled(bool toggledOn, FloorPaintMode mode)
        {
            if (toggledOn)
            {
                UpdateFloorPickerVisibility(mode == FloorPaintMode.Docks);
                PaintModeChanged?.Invoke(mode);
            }
        }

        // Docks mode swaps the categorized tree for a flat list of dockable floors (roofs-style).
        private void UpdateFloorPickerVisibility(bool docksMode)
        {
            _floorsTree.gameObject.SetActive(!docksMode);
            _dockFloorsList.gameObject.SetActive(docksMode);
        }

        private void OnDockSupportToggled(bool toggledOn, bool auto, DockSupportData support)
        {
            if (toggledOn)
            {
                DockSupportChanged?.Invoke(auto, support);
            }
        }

        private void OnStoneSupportToggled(bool toggledOn)
        {
            _stoneVariantRow.SetActive(toggledOn);
            if (toggledOn)
            {
                Toggle activeVariant = Array.Find(_stoneVariantToggles, toggle => toggle.isOn);
                int index = Array.IndexOf(_stoneVariantToggles, activeVariant);
                if (index < 0)
                {
                    index = 0;
                }

                DockSupportChanged?.Invoke(false, Database.DockSupports[StoneVariantShortNames[index]]);
            }
        }
    }
}
