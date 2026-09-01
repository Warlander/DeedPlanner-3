using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Ui
{
    public class ObservableToggleGroup : ToggleGroup
    {
        public event Action<Toggle> ActiveToggleChanged;

        private struct ToggleListener
        {
            public Toggle Toggle;
            public UnityAction<bool> Listener;
        }

        private ToggleListener[] _toggleListeners;

        protected override void Start()
        {
            base.Start();

            _toggleListeners = new ToggleListener[m_Toggles.Count];
            int index = 0;
            foreach (Toggle toggle in m_Toggles)
            {
                if (toggle.group != this)
                    continue;
                Toggle captured = toggle;
                UnityAction<bool> listener = isOn =>
                {
                    if (isOn)
                        ActiveToggleChanged?.Invoke(captured);
                };
                toggle.onValueChanged.AddListener(listener);
                _toggleListeners[index++] = new ToggleListener { Toggle = captured, Listener = listener };
            }
        }

        // Skip base: EnsureValidState force-selects a wrong toggle when group re-activates before it re-registers.
        protected override void OnEnable()
        {
        }

        protected override void OnDestroy()
        {
            if (_toggleListeners == null)
                return;

            foreach (ToggleListener entry in _toggleListeners)
            {
                if (entry.Toggle != null)
                    entry.Toggle.onValueChanged.RemoveListener(entry.Listener);
            }
            
            base.OnDestroy();
        }
    }
}
