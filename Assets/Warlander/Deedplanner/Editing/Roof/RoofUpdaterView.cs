using System;
using UnityEngine;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class RoofUpdaterView : MonoBehaviour, IRoofUpdaterView
    {
        [SerializeField] private UnityList _roofsList;

        public event Action<RoofData> RoofSelected;

        private void Awake()
        {
            _roofsList.ValueChanged += OnRoofsListValueChanged;
        }

        public void AddRoofEntry(RoofData data)
        {
            _roofsList.Add(data);
        }

        public void PushSelection()
        {
            OnRoofsListValueChanged(_roofsList.SelectedValue);
        }

        private void OnRoofsListValueChanged(object value)
        {
            RoofSelected?.Invoke(value as RoofData);
        }
    }
}
