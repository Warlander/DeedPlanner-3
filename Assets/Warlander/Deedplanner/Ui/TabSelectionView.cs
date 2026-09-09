using Warlander.Deedplanner.Editing;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Warlander.Deedplanner.Ui
{
    public class TabSelectionView : MonoBehaviour, ITabSelectionView
    {
        [SerializeField] private ObservableToggleGroup _tabToggleGroup = null;

        private bool _updatingSelection;

        public event Action<Tab> TabSelected;

        private void Start()
        {
            _tabToggleGroup.ActiveToggleChanged += OnActiveToggleChanged;
        }

        private void OnActiveToggleChanged(Toggle toggle)
        {
            if (_updatingSelection)
                return;

            if (toggle.TryGetComponent(out TabReference tabReference))
            {
                TabSelected?.Invoke(tabReference.Tab);
            }
        }

        public void SelectTab(Tab tab)
        {
            Toggle selectedToggle = null;
            foreach (Toggle toggle in _tabToggleGroup.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.TryGetComponent(out TabReference tabReference) && tabReference.Tab == tab)
                {
                    selectedToggle = toggle;
                    break;
                }
            }

            if (!selectedToggle)
            {
                return;
            }

            bool allowSwitchOff = _tabToggleGroup.allowSwitchOff;
            _updatingSelection = true;
            try
            {
                _tabToggleGroup.allowSwitchOff = true;
                foreach (Toggle toggle in _tabToggleGroup.GetComponentsInChildren<Toggle>(true))
                {
                    if (toggle.TryGetComponent<TabReference>(out _))
                    {
                        toggle.isOn = toggle == selectedToggle;
                    }
                }
            }
            finally
            {
                _tabToggleGroup.allowSwitchOff = allowSwitchOff;
                _updatingSelection = false;
            }
        }

        private void OnDestroy()
        {
            if (_tabToggleGroup)
            {
                _tabToggleGroup.ActiveToggleChanged -= OnActiveToggleChanged;
            }
        }
    }
}
