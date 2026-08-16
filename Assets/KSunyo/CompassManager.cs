using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using VContainer;


namespace KSunyo
{
    public class CompassManager : MonoBehaviour
    {
        [Inject] private DPSettings _settings;
        [Inject] private DPInput _input;

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private RectTransform compassFront;
        [SerializeField] private GameObject compass;

        private bool _uiVisible = true;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            _input.EditingControls.ToggleUI.started += OnToggleUI;
        }

        private void OnDestroy()
        {
            _input.EditingControls.ToggleUI.started -= OnToggleUI;
        }

        private void OnToggleUI(InputAction.CallbackContext context)
        {
            _uiVisible = !_uiVisible;
        }

        void Update()
        {
            bool isCameraActive = cameraTransform != null && cameraTransform.gameObject.activeInHierarchy;
            if (isCameraActive)
            {   
                compass.SetActive(true);
                float camY = cameraTransform.eulerAngles.y;
                float targetZ = 360f + camY;
                compassFront.localRotation = Quaternion.Euler(0, 0, targetZ);
            }
            else
            {
                compass.SetActive(false);
            }

            if (_settings.CompassVisibility && isCameraActive && _uiVisible)
            {
                compass.SetActive(true);
            }
            else
            {
                compass.SetActive(false);
            }
        }
    }
}
