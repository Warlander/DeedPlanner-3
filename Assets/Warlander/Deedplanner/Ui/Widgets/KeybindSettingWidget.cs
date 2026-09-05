using System;
using UnityEngine;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Windows;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public sealed class KeybindSettingWidget : MonoBehaviour, ISettingWidget
    {
        [SerializeField] private KeybindSettingRow _row;

        private KeybindSetting _setting;
        private IRebindOverlay _overlay;

        public event Action ValueEdited;

        public RectTransform Root => (RectTransform) transform;

        private void Awake()
        {
            _row.RebindButton.onClick.AddListener(OnRebindClick);
        }

        public void Bind(KeybindSetting setting, IRebindOverlay overlay)
        {
            _setting = setting;
            _overlay = overlay;
            _setting.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            if (_setting == null)
            {
                return;
            }
            _row.SetLabels(_setting.Label, _setting.DisplayString);
        }

        private void OnRebindClick()
        {
            _overlay.ShowRebind(_setting.Label);
            _setting.PerformInteractiveRebind(
                () =>
                {
                    _overlay.HideRebind();
                    ValueEdited?.Invoke();
                },
                () => _overlay.HideRebind());
        }

        private void OnDestroy()
        {
            if (_setting != null)
            {
                _setting.Changed -= Refresh;
            }
        }
    }
}
