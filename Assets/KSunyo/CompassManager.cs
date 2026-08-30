using UnityEngine;
using Warlander.Deedplanner.Settings;
using Warlander.UI;
using VContainer;


namespace KSunyo
{
    public class CompassManager : MonoBehaviour
    {
        [Inject] private DPSettings _settings;
        [Inject] private IInterfaceVisibility _interfaceVisibility;

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private RectTransform compassFront;
        [SerializeField] private GameObject compass;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
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

            if (_settings.CompassVisibility && isCameraActive && _interfaceVisibility.Visible)
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
