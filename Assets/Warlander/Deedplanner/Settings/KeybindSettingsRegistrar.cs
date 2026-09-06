using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Warlander.Deedplanner.Inputs;
using Warlogic.Settings;
using InputSettings = Warlander.Deedplanner.Settings.InputSettings;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// Declares the Keybinds tab on the project-wide registry.
    /// </summary>
    public sealed class KeybindSettingsRegistrar : IInitializable, IDisposable
    {
        private readonly SettingsRegistry _registry;
        private readonly DPInput _input;
        private readonly InputSettings _inputSettings;
        private readonly List<KeybindSetting> _settings = new List<KeybindSetting>();

        public KeybindSettingsRegistrar(SettingsRegistry registry, DPInput input, InputSettings inputSettings)
        {
            _registry = registry;
            _input = input;
            _inputSettings = inputSettings;
        }

        public void Initialize()
        {
            SettingsTab keybindsTab = _registry.AddTab("keybinds", "Keybinds", 100);
            foreach (InputActionMap actionMap in _input.asset.actionMaps)
            {
                // UI map stays non-rebindable so the user can't lock themselves out of the UI
                if (actionMap == _input.UI.Submit.actionMap)
                {
                    continue;
                }

                foreach (InputAction action in actionMap.actions)
                {
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        InputBinding binding = action.bindings[i];
                        if (binding.isComposite)
                        {
                            continue;
                        }

                        var setting = new KeybindSetting(action, i, _inputSettings);
                        _settings.Add(setting);
                        keybindsTab.Add(setting);

                        if (!binding.isPartOfComposite)
                        {
                            // standalone actions show only their first binding
                            break;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (KeybindSetting setting in _settings)
            {
                setting.Dispose();
            }
            _settings.Clear();
        }
    }
}
