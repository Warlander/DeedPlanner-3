using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using VContainer;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public class TabSelectionHandler : MonoBehaviour
    {
        [SerializeField] private ObservableToggleGroup tabToggleGroup = null;

        private TabContext _tabContext;

        [Inject]
        private void Inject(TabContext tabContext)
        {
            _tabContext = tabContext;
        }

        private void Start()
        {
            tabToggleGroup.ActiveToggleChanged += OnTabToggleActiveChanged;
        }

        private void OnTabToggleActiveChanged(Toggle toggle)
        {
            if (toggle.TryGetComponent(out TabReference tabReference))
            {
                _tabContext.CurrentTab = tabReference.Tab;
            }
        }

        private void OnDestroy()
        {
            if (tabToggleGroup)
            {
                tabToggleGroup.ActiveToggleChanged -= OnTabToggleActiveChanged;
            }
        }
    }
}
