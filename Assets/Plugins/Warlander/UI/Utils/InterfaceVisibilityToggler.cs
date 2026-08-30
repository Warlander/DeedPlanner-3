using UnityEngine;
using Warlander.Core;
using VContainer;

namespace Warlander.UI.Utils
{
    public class InterfaceVisibilityToggler : WarlanderBehaviour
    {
        private IInterfaceVisibility _interfaceVisibility;
        private bool _hiddenByToggler;

        [Inject]
        private void Injected(IInterfaceVisibility interfaceVisibility)
        {
            _interfaceVisibility = interfaceVisibility;
            _interfaceVisibility.VisibilityChanged += OnVisibilityChanged;
        }

        private void OnVisibilityChanged(bool visible)
        {
            if (visible)
            {
                if (_hiddenByToggler)
                {
                    gameObject.SetActive(true);
                    _hiddenByToggler = false;
                }
            }
            else
            {
                _hiddenByToggler = gameObject.activeSelf;
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_interfaceVisibility != null)
            {
                _interfaceVisibility.VisibilityChanged -= OnVisibilityChanged;
            }
        }
    }
}
