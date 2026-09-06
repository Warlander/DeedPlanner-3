using System;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using VContainer.Unity;
using Warlander.Deedplanner.Logging;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public class InputSettings : IInitializable, IDisposable
    {
        private readonly DPInput _input;
        private readonly ISettingsStore _store;
        private readonly ICategoryLogger _logger;

        public event Action SettingsReset;

        public InputSettings(DPInput input, ISettingsStore store, ILoggerSource loggerSource)
        {
            _input = input;
            _store = store;
            _logger = loggerSource.Create(DeedPlannerSettings.Category);
        }

        void IInitializable.Initialize()
        {
            if (_store.TryLoad(LegacySettingsMigration.BindingOverridesKey, out string bindingOverrides))
            {
                try
                {
                    _input.LoadBindingOverridesFromJson(bindingOverrides);
                    DeleteLegacyBindingOverrides();
                }
                catch (Exception exception)
                {
                    _input.RemoveAllBindingOverrides();
                    _logger.Warning($"Ignored invalid saved keybind overrides: {exception.Message}");
                }
            }

            _input.Enable();
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

        public void Dispose()
        {
            _input.Disable();
            _input.Dispose();
        }

        private static void DeleteLegacyBindingOverrides()
        {
            if (!UnityEngine.PlayerPrefs.HasKey(LegacySettingsMigration.LegacyInputSettingsKey))
            {
                return;
            }

            UnityEngine.PlayerPrefs.DeleteKey(LegacySettingsMigration.LegacyInputSettingsKey);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
