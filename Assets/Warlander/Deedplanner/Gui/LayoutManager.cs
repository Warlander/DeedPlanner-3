using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class LayoutManager : MonoBehaviour
    {
        [SerializeField] private Toggle[] indicatorButtons = new Toggle[4];
        [SerializeField] private RectTransform horizontalBottomIndicatorHolder = null;
        [SerializeField] private RawImage[] screens = new RawImage[4];
        [SerializeField] private RectTransform horizontalBottomScreenHolder = null;
        [SerializeField] private RectTransform[] splits = new RectTransform[5];

        [SerializeField] private Toggle groundToggle = null;
        [SerializeField] private Toggle cavesToggle = null;
        [SerializeField] private ObservableToggleGroup tabToggleGroup = null;

        public Layout CurrentLayout { get; private set; } = Layout.Single;

        private CameraCoordinator _cameraCoordinator;
        private TabContext _tabContext;

        [Inject]
        private void Inject(CameraCoordinator cameraCoordinator, TabContext tabContext)
        {
            _cameraCoordinator = cameraCoordinator;
            _tabContext = tabContext;
        }

        private void Start()
        {
            ChangeLayout(CurrentLayout);

            _cameraCoordinator.CurrentCameraChanged += CameraCoordinatorOnCurrentCameraChanged;
            _tabContext.TabChanged += TabContextOnTabChanged;
            tabToggleGroup.ActiveToggleChanged += OnTabToggleActiveChanged;
        }

        private void CameraCoordinatorOnCurrentCameraChanged()
        {
            OnActiveWindowChange();
        }
        
        private void TabContextOnTabChanged(Tab obj)
        {
            UpdateTabToggles();
        }

        public void ChangeLayout(Layout layout)
        {
            CurrentLayout = layout;

            switch (layout)
            {
                case Layout.Single:
                    ToggleMainScreenObjects(true, false, false, false);
                    break;
                case Layout.HorizontalSplit:
                    ToggleMainScreenObjects(true, false, true, false);
                    break;
                case Layout.VerticalSplit:
                    ToggleMainScreenObjects(true, true, false, false);
                    break;
                case Layout.HorizontalTop:
                    ToggleMainScreenObjects(true, false, true, true);
                    break;
                case Layout.HorizontalBottom:
                    ToggleMainScreenObjects(true, true, true, false);
                    break;
                case Layout.Quad:
                    ToggleMainScreenObjects(true, true, true, true);
                    break;
            }
        }

        private void ToggleMainScreenObjects(bool topRightWindowVisible, bool topLeftWindowVisible, bool bottomRightWindowVisible, bool bottomLeftWindowVisible)
        {
            ToggleSharedMainScreenObjects(0, topRightWindowVisible);
            ToggleSharedMainScreenObjects(1, topLeftWindowVisible);
            ToggleSharedMainScreenObjects(2, bottomRightWindowVisible);
            ToggleSharedMainScreenObjects(3, bottomLeftWindowVisible);

            bool bottomRowVisible = bottomRightWindowVisible || bottomLeftWindowVisible;
            horizontalBottomIndicatorHolder.gameObject.SetActive(bottomRowVisible);
            horizontalBottomScreenHolder.gameObject.SetActive(bottomRowVisible);

            bool onlyOneTopActive = topRightWindowVisible ^ topLeftWindowVisible;
            bool onlyOneBottomActive = bottomRightWindowVisible ^ bottomLeftWindowVisible;
            bool onlyOneLeftActive = topLeftWindowVisible ^ bottomLeftWindowVisible;
            bool onlyOneRightActive = topRightWindowVisible ^ bottomRightWindowVisible;

            // "-" shaped split
            splits[0].gameObject.SetActive(onlyOneTopActive && onlyOneBottomActive);
            // "|" shaped split
            splits[1].gameObject.SetActive(onlyOneLeftActive && onlyOneRightActive);
            // "T" shaped split
            splits[2].gameObject.SetActive(onlyOneTopActive && bottomRightWindowVisible && bottomLeftWindowVisible);
            // Reverse "T" shaped split
            splits[3].gameObject.SetActive(topRightWindowVisible && topLeftWindowVisible && onlyOneBottomActive);
            // "+" shaped split
            splits[4].gameObject.SetActive(topRightWindowVisible && topLeftWindowVisible && bottomRightWindowVisible && bottomLeftWindowVisible);
        }

        private void ToggleSharedMainScreenObjects(int index, bool enable)
        {
            _cameraCoordinator.ToggleCamera(index, enable);
            indicatorButtons[index].gameObject.SetActive(enable);
            screens[index].gameObject.SetActive(enable);

            // if screen is being toggled off, focus primary screen instead
            if (!enable && _cameraCoordinator.ActiveId == index)
            {
                indicatorButtons[_cameraCoordinator.ActiveId].isOn = false;
                _cameraCoordinator.ChangeCurrentCamera(0);
                indicatorButtons[0].isOn = true;
            }
        }

        public void OnActiveIndicatorChange(int window)
        {
            if (indicatorButtons[window].isOn)
            {
                _cameraCoordinator.ChangeCurrentCamera(window);
                Debug.Log("Active window changed to " + window);
            }
        }

        private void OnActiveWindowChange()
        {
            int activeId = _cameraCoordinator.ActiveId;

            for (int i = 0; i < indicatorButtons.Length; i++)
            {
                Toggle indicatorButton = indicatorButtons[i];
                indicatorButton.isOn = activeId == i;
            }
        }

        private void OnTabToggleActiveChanged(Toggle toggle)
        {
            if (toggle.TryGetComponent(out TabReference tabReference))
            {
                _tabContext.CurrentTab = tabReference.Tab;
                UpdateTabToggles();
            }
        }

        private void UpdateTabToggles()
        {
            int floor = _cameraCoordinator.Current.Level;
            if (floor < 0)
            {
                groundToggle.gameObject.SetActive(false);
                cavesToggle.gameObject.SetActive(true);
                if (groundToggle.isOn)
                {
                    groundToggle.isOn = false;
                    cavesToggle.isOn = true;
                }
            }
            else if (floor >= 0)
            {
                groundToggle.gameObject.SetActive(true);
                cavesToggle.gameObject.SetActive(false);
                if (cavesToggle.isOn)
                {
                    groundToggle.isOn = true;
                    cavesToggle.isOn = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (tabToggleGroup)
            {
                tabToggleGroup.ActiveToggleChanged -= OnTabToggleActiveChanged;
            }
            
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _tabContext.TabChanged -= TabContextOnTabChanged;
        }
    }
}
