using System;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using VContainer;
using VContainer.Unity;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public class InputSettings : IInitializable
    {
        [Inject] private DPInput _input;
        [Inject] private ISettingsStore _store;

        public event Action SettingsReset;

        void IInitializable.Initialize()
        {
            if (_store.TryLoad(LegacySettingsMigration.BindingOverridesKey, out string bindingOverrides))
            {
                _input.LoadBindingOverridesFromJson(bindingOverrides);
            }
        }

        public void Save()
        {
            _store.Save(LegacySettingsMigration.BindingOverridesKey, _input.SaveBindingOverridesAsJson());
        }

        public void Reset()
        {
            _input.RemoveAllBindingOverrides();
            Save();
            SettingsReset?.Invoke();
        }
    }
}
