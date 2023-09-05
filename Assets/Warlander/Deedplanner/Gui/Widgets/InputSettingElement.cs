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
        
        public void Set(InputAction inputAction)
        {
            _actionNameText.text = inputAction.name;
            _currentKeybindText.text = inputAction.GetBindingDisplayString();
        }
    }
}