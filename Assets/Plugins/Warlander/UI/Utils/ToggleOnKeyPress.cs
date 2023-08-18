#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Core;
using Zenject;

namespace Warlander.UI.Utils
{
    public class ToggleOnKeyPress : WarlanderBehaviour
    {
        [SerializeField] private InputActionReference _toggle;

        private bool _initialized;
        
        /// <summary>
        /// Injection so the method will be executed even if object is disabled.
        /// </summary>
        [Inject]
        private void Injected()
        {
            Init();
        }

        private void Awake()
        {
            Init();
        }
        
        private void Init()
        {
            if (_initialized == false)
            {
                _toggle.action.Enable();
                _toggle.action.started += ActionOnStarted;
                _initialized = true;
            }
        }

        private void ActionOnStarted(InputAction.CallbackContext obj)
        {
            gameObject.SetActive(gameObject.activeSelf == false);
        }

        private void OnDestroy()
        {
            _toggle.action.started -= ActionOnStarted;
        }
    }
}
#endif