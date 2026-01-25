using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class LayoutChangeToggle : MonoBehaviour
    {
        [Inject] private LayoutManager _layoutManager;

        [SerializeField] private Toggle _toggle;
        [SerializeField] private Layout _layout;

        private void Start()
        {
            _toggle.isOn = (_layoutManager.currentLayout == _layout);
            _toggle.onValueChanged.AddListener(ToggleOnValueChanged);
        }

        private void ToggleOnValueChanged(bool toggled)
        {
            if (toggled)
            {
                _layoutManager.ChangeLayout(_layout);
            }
        }
    }
}