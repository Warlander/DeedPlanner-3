using System;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public class TabSelectionView : MonoBehaviour, ITabSelectionView
    {
        [SerializeField] private ObservableToggleGroup _tabToggleGroup = null;

        public event Action<Tab> TabSelected;

        private void Start()
        {
            _tabToggleGroup.ActiveToggleChanged += OnActiveToggleChanged;
        }

        private void OnActiveToggleChanged(Toggle toggle)
        {
            if (toggle.TryGetComponent(out TabReference tabReference))
            {
                TabSelected?.Invoke(tabReference.Tab);
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
