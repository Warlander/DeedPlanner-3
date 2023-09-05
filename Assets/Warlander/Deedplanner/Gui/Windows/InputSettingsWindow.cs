using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class InputSettingsWindow : MonoBehaviour
    {
        [Inject] private DPInput _input;
        [Inject] private Window _window;

        [SerializeField] private Transform _settingsContent;
        [SerializeField] private TMP_Text _inputMapNamePrefab;
        [SerializeField] private InputSettingElement _inputElementPrefab;

        private void Start()
        {
            _inputElementPrefab.gameObject.SetActive(false);
            _inputMapNamePrefab.gameObject.SetActive(false);

            foreach (InputActionMap inputMap in _input.asset.actionMaps)
            {
                // Ignore input action map.
                if (inputMap == _input.UI.Submit.actionMap)
                {
                    continue;
                }

                TMP_Text inputMapNameText = Instantiate(_inputMapNamePrefab, _settingsContent);
                inputMapNameText.text = inputMap.name;
                inputMapNameText.gameObject.SetActive(true);
                
                foreach (InputAction action in inputMap.actions)
                {
                    InputSettingElement newElement = Instantiate(_inputElementPrefab, _settingsContent);
                    newElement.Set(action);
                    newElement.gameObject.SetActive(true);
                }
            }
        }
    }
}