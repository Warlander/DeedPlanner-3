using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public class FloorUpdaterView : MonoBehaviour, IFloorUpdaterView
    {
        [SerializeField] private UnityTree _floorsTree;

        [SerializeField] private Toggle _southToggle;
        [SerializeField] private Toggle _westToggle;
        [SerializeField] private Toggle _northToggle;
        [SerializeField] private Toggle _eastToggle;

        public event Action<FloorData> FloorSelected;
        public event Action<EntityOrientation> OrientationChanged;

        private void Awake()
        {
            _floorsTree.ValueChanged += OnFloorsTreeValueChanged;
            _southToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Down));
            _westToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Right));
            _northToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Up));
            _eastToggle.onValueChanged.AddListener(toggled => OnOrientationToggled(toggled, EntityOrientation.Left));
        }

        public void AddFloorEntry(FloorData data, string[] category)
        {
            _floorsTree.Add(data, category);
        }

        public void PushSelection()
        {
            OnFloorsTreeValueChanged(_floorsTree.SelectedValue);
        }

        private void OnFloorsTreeValueChanged(object value)
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
    }
}
