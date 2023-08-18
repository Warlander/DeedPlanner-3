#if ENABLE_INPUT_SYSTEM
using System;
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
            _toggle.action.performed += ActionOnperformed;
            _initialized = true;
        }

        private void ActionOnperformed(InputAction.CallbackContext obj)
        {
            gameObject.SetActive(gameObject.activeSelf == false);
        }

        private void OnDestroy()
        {
            _toggle.action.performed -= ActionOnperformed;
        }
    }
}
#endif