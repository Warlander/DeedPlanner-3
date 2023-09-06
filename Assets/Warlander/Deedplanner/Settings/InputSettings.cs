using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using Zenject;

namespace Warlander.Deedplanner.Settings
{
    public class InputSettings : IInitializable
    {
        public const string InputSettingsKey = "inputSettings";
        
        [Inject] private DPInput _input;

        void IInitializable.Initialize()
        {
            if (PlayerPrefs.HasKey(InputSettingsKey))
            {
                string bindingOverrides = PlayerPrefs.GetString(InputSettingsKey);
                _input.LoadBindingOverridesFromJson(bindingOverrides);
            }
        }

        public void Save()
        {
            PlayerPrefs.SetString(InputSettingsKey, _input.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }
    }
}