using UnityEngine;
using UnityEngine.UI;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class SidePanelManager : MonoBehaviour
    {
        [Inject] private WindowCoordinator _windowCoordinator;
        
        [SerializeField] private Button _layoutsButton;

        private void Start()
        {
            _layoutsButton.onClick.AddListener(LayoutsButtonOnClick);
        }

        private void LayoutsButtonOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.LayoutWindow);
        }
    }
}