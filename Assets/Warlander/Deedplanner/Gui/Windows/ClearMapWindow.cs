using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Warlander.Deedplanner.Logic;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class ClearMapWindow : MonoBehaviour
    {
        private Window _window;
        [Inject] private MapHandler _mapHandler;

        [SerializeField] private Button _clearMapButton;
        [SerializeField] private Button _cancelButton;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

        private void Start()
        {
            _clearMapButton.onClick.AddListener(ClearMapOnClick);
            _cancelButton.onClick.AddListener(CancelOnClick);
        }

        private void ClearMapOnClick()
        {
            _mapHandler.ClearMap();
            _window.Close();
        }

        private void CancelOnClick()
        {
            _window.Close();
        }
    }
}