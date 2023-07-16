using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class ClearMapWindow : MonoBehaviour
    {
        [Inject] private Window _window;
        
        [SerializeField] private Button _clearMapButton;
        [SerializeField] private Button _cancelButton;

        private void Start()
        {
            _clearMapButton.onClick.AddListener(ClearMapOnClick);
            _cancelButton.onClick.AddListener(CancelOnClick);
        }

        private void ClearMapOnClick()
        {
            GameManager.Instance.ClearMap();
            _window.Close();
        }

        private void CancelOnClick()
        {
            _window.Close();
        }
    }
}