using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class VisibleTabsToggler : MonoBehaviour
    {
        [SerializeField] private Toggle groundToggle = null;
        [SerializeField] private Toggle cavesToggle = null;

        private CameraCoordinator _cameraCoordinator;

        [Inject]
        private void Inject(CameraCoordinator cameraCoordinator)
        {
            _cameraCoordinator = cameraCoordinator;
        }

        private void Start()
        {
            _cameraCoordinator.LevelChanged += OnLevelChanged;
            OnLevelChanged();
        }

        private void OnLevelChanged()
        {
            bool caves = _cameraCoordinator.Current.Level < 0;
            groundToggle.gameObject.SetActive(!caves);
            cavesToggle.gameObject.SetActive(caves);
            if (caves)
                cavesToggle.isOn = true;
            else
                groundToggle.isOn = true;
        }

        private void OnDestroy()
        {
            _cameraCoordinator.LevelChanged -= OnLevelChanged;
        }
    }
}
