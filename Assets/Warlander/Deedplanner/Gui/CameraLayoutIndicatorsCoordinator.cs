using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class CameraLayoutIndicatorsCoordinator : MonoBehaviour
    {
        [SerializeField] private Toggle[] indicatorButtons = new Toggle[4];

        private LayoutManager _layoutManager;
        private CameraCoordinator _cameraCoordinator;

        [Inject]
        private void Inject(LayoutManager layoutManager, CameraCoordinator cameraCoordinator)
        {
            _layoutManager = layoutManager;
            _cameraCoordinator = cameraCoordinator;
        }

        private void Start()
        {
            _layoutManager.LayoutChanged += OnLayoutChanged;
            _cameraCoordinator.CurrentCameraChanged += OnActiveWindowChange;

            OnLayoutChanged(_layoutManager.CurrentLayout);
            OnActiveWindowChange();
        }

        public void OnActiveIndicatorChange(int window)
        {
            if (indicatorButtons[window].isOn)
            {
                _cameraCoordinator.ChangeCurrentCamera(window);
                Debug.Log("Active window changed to " + window);
            }
        }

        private void OnLayoutChanged(Layout layout)
        {
            bool[] visible = layout switch
            {
                Layout.Single         => new[] { true,  false, false, false },
                Layout.HorizontalSplit => new[] { true,  false, true,  false },
                Layout.VerticalSplit   => new[] { true,  true,  false, false },
                Layout.HorizontalTop   => new[] { true,  false, true,  true  },
                Layout.HorizontalBottom => new[] { true,  true,  true,  false },
                Layout.Quad            => new[] { true,  true,  true,  true  },
                _                      => new[] { true,  false, false, false },
            };

            for (int i = 0; i < indicatorButtons.Length; i++)
            {
                indicatorButtons[i].gameObject.SetActive(visible[i]);
            }
        }

        private void OnActiveWindowChange()
        {
            int activeId = _cameraCoordinator.ActiveId;

            for (int i = 0; i < indicatorButtons.Length; i++)
            {
                indicatorButtons[i].isOn = activeId == i;
            }
        }

        private void OnDestroy()
        {
            _layoutManager.LayoutChanged -= OnLayoutChanged;
            _cameraCoordinator.CurrentCameraChanged -= OnActiveWindowChange;
        }
    }
}
