using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.UI.Windows;
using Zenject;
using InputSettings = Warlander.Deedplanner.Settings.InputSettings;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class InputSettingsWindow : MonoBehaviour
    {
        [Inject] private DPInput _input;
        [Inject] private InputSettings _inputSettings;
        [Inject] private Window _window;

        [SerializeField] private CanvasGroup _rebindFade;
        [SerializeField] private TMP_Text _rebindText;
        
        [SerializeField] private Transform _settingsContent;
        [SerializeField] private TMP_Text _inputMapNamePrefab;
        [SerializeField] private InputSettingElement _inputElementPrefab;

        [SerializeField] private Button _resetBindingsButton;

        private List<InputSettingElement> _inputElements;

        private void Start()
        {
            _rebindFade.gameObject.SetActive(false);
            _inputElementPrefab.gameObject.SetActive(false);
            _inputMapNamePrefab.gameObject.SetActive(false);

            _resetBindingsButton.onClick.AddListener(ResetBindingsOnClick);

            _inputElements = new List<InputSettingElement>();
            
            foreach (InputActionMap inputMap in _input.asset.actionMaps)
            {
                // We don't want to introduce deadlock situation where user changes/disables important UI input
                // and can't use UI anymore - disable customizing it. This might be re-considered on users request.
                if (inputMap == _input.UI.Submit.actionMap)
                {
                    continue;
                }

                TMP_Text inputMapNameText = Instantiate(_inputMapNamePrefab, _settingsContent);
                inputMapNameText.text = inputMap.name;
                inputMapNameText.gameObject.SetActive(true);
                
                foreach (InputAction action in inputMap.actions)
                {
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        InputBinding binding = action.bindings[i];
                        if (binding.isComposite)
                        {
                            continue;
                        }
                        
                        InputSettingElement newElement = Instantiate(_inputElementPrefab, _settingsContent);
                        newElement.Set(action, i);
                        newElement.RebindStarted += NewElementOnRebindStarted;
                        newElement.RebindSuccess += NewElementOnRebindSuccess;
                        newElement.RebindFailed += NewElementOnRebindFailed;
                        newElement.gameObject.SetActive(true);
                        _inputElements.Add(newElement);

                        if (binding.isComposite == false && binding.isPartOfComposite == false)
                        {
                            // Don't add duplicate bindings to the list.
                            break;
                        }
                    }
                }
            }
            
            _inputSettings.SettingsReset += InputSettingsOnSettingsReset;
        }

        private void InputSettingsOnSettingsReset()
        {
            foreach (InputSettingElement inputElement in _inputElements)
            {
                inputElement.RefreshUI();
            }
        }

        private void NewElementOnRebindStarted(InputSettingElement rebindElement)
        {
            ShowRebind(rebindElement.GetBindingName());
        }
        
        private void NewElementOnRebindSuccess()
        {
            HideRebind();
            _inputSettings.Save();
        }
        
        private void NewElementOnRebindFailed()
        {
            HideRebind();
        }

        private void ShowRebind(string bindingName)
        {
            _rebindText.text = $"Waiting for input to rebind\n\n{bindingName}";
            _rebindFade.gameObject.SetActive(true);
            _rebindFade.alpha = 0;
            _rebindFade.DOFade(1, 0.15f);
        }

        private void HideRebind()
        {
            _rebindFade.DOFade(0, 0.15f).OnComplete(() => _rebindFade.gameObject.SetActive(false));
        }
        
        private void ResetBindingsOnClick()
        {
            _inputSettings.Reset();
        }

        private void OnDestroy()
        {
            _rebindFade.DOKill();
            
            _inputSettings.SettingsReset -= InputSettingsOnSettingsReset;
        }
    }
}