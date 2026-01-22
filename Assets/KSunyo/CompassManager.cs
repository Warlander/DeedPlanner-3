using UnityEngine;
using Warlander.Deedplanner.Settings;
using Zenject;


namespace KSunyo
{
    public class CompassManager : MonoBehaviour
    {
        [Inject] private DPSettings _settings;
        
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

            if (_settings.CompassVisibility && isCameraActive)
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
