using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Warlander.Deedplanner.Ui
{
    public class LayoutChangeToggle : MonoBehaviour
    {
        [Inject] private LayoutContext _layoutContext;

        [SerializeField] private Toggle _toggle;
        [SerializeField] private Layout _layout;

        private void Start()
        {
            _toggle.isOn = (_layoutContext.CurrentLayout == _layout);
            _toggle.onValueChanged.AddListener(ToggleOnValueChanged);
        }

        private void ToggleOnValueChanged(bool toggled)
        {
            if (toggled)
            {
                _layoutContext.ChangeLayout(_layout);
            }
        }
    }
}
