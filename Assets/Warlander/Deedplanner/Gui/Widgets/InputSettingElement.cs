using System;
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

        public event Action<InputSettingElement> RebindStarted;
        public event Action RebindSuccess;
        public event Action RebindFailed;
        
        private InputAction _inputAction;
        private int _bindingIndex;
        
        private void Awake()
        {
            _changeBindingButton.onClick.AddListener(OnChangeBindingClick);
        }
        
        public void Set(InputAction inputAction, int bindingIndex)
        {
            _inputAction = inputAction;
            _bindingIndex = bindingIndex;
            
            _actionNameText.text = GetBindingName();
            _currentKeybindText.text = inputAction.bindings[bindingIndex].ToDisplayString();
        }

        public string GetBindingName()
        {
            string bindingName = _inputAction.bindings[_bindingIndex].name;
            if (string.IsNullOrEmpty(bindingName) == false)
            {
                return $"{_inputAction.name} ({bindingName})";
            }
            else
            {
                return _inputAction.name;
            }
        }

        private void OnChangeBindingClick()
        {
            _inputAction.Disable();
            
            _inputAction.PerformInteractiveRebinding(_bindingIndex).OnComplete(operation =>
            {
                Set(_inputAction, _bindingIndex);
                operation.Dispose();
                _inputAction.Enable();
                RebindSuccess?.Invoke();
            }).OnCancel(operation =>
            {
                operation.Dispose();
                _inputAction.Enable();
                RebindFailed?.Invoke();
            }).Start();
            
            RebindStarted?.Invoke(this);
        }
    }
}