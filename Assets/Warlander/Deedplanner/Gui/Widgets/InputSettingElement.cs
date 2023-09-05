using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public class InputSettingElement : MonoBehaviour
    {
        [SerializeField] private TMP_Text _actionNameText;
        [SerializeField] private TMP_Text _currentKeybindText;
        [SerializeField] private Button _changeBindingButton;

        private InputAction _inputAction;
        
        private void Awake()
        {
            _changeBindingButton.onClick.AddListener(OnChangeBindingClick);
        }
        
        public void Set(InputAction inputAction)
        {
            _inputAction = inputAction;
            
            _actionNameText.text = inputAction.name;
            _currentKeybindText.text = inputAction.GetBindingDisplayString();
        }

        private void OnChangeBindingClick()
        {
            _inputAction.Disable();
            
           _inputAction.PerformInteractiveRebinding().OnComplete(operation =>
           {
               _inputAction.Enable();
               Set(_inputAction);
               operation.Dispose();
           }).OnCancel(operation =>
           {
               _inputAction.Enable();
               operation.Dispose();
           }).Start();
        }
    }
}