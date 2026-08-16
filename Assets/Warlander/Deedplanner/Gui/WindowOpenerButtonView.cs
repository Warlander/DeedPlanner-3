using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class WindowOpenerButtonView : MonoBehaviour, IWindowOpenerButtonView
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _windowName;
        [SerializeField] private bool _exclusive = true;

        public event Action<WindowOpenRequest> WindowOpenRequested;

        private void Start()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            WindowOpenRequested?.Invoke(new WindowOpenRequest(_windowName, _exclusive));
        }
    }
}
