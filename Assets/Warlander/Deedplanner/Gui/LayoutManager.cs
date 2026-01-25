using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using System.Linq;
using System;
using DG.Tweening;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class LayoutManager : MonoBehaviour
    {

        [Inject] private DPSettings _settings;
        [Inject] private CameraCoordinator _cameraCoordinator;
        
        [SerializeField] private Toggle[] indicatorButtons = new Toggle[4];
        [SerializeField] private RectTransform horizontalBottomIndicatorHolder = null;
        [SerializeField] private RawImage[] screens = new RawImage[4];
        [SerializeField] private RectTransform horizontalBottomScreenHolder = null;
        [SerializeField] private RectTransform[] splits = new RectTransform[5];

        [SerializeField] private UIContentTab[] tabs = new UIContentTab[12];
        [SerializeField] private Toggle groundToggle = null;
        [SerializeField] private Toggle cavesToggle = null;

        [SerializeField] private GameObject highQualityWaterObject = null;
        [SerializeField] private GameObject simpleQualityWaterObject = null;

        public event Action<Tab> TabChanged;
        public static LayoutManager Instance { get; private set; }
        public TileSelectionMode TileSelectionMode { get; set; }
        public Layout CurrentLayout { get; private set; } = Layout.Single;
        public Tab CurrentTab {
            get => currentTab;
            set
            {
                CreateAndStartTabFadeAnimation(currentTab, value);
                
                currentTab = value;
                TabChanged?.Invoke(currentTab);
            }
        }

        private int activeWindow;
        private Tab currentTab;
        private Sequence tabFadeSequence;
        
        public LayoutManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // state validation at launch - it makes development and debugging easier as you don't need to make sure tab is set to the proper one when commiting
            CurrentTab = currentTab;
            
            ValidateState();
            ChangeLayout(CurrentLayout);
            
            _settings.Modified += ValidateState;
            _cameraCoordinator.CurrentCameraChanged += CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged += CameraCoordinatorOnLevelChanged;
        }

        private void CameraCoordinatorOnCurrentCameraChanged()
        {
            OnActiveWindowChange();
        }

        private void CameraCoordinatorOnLevelChanged()
        {
            UpdateTabs();
        }

        private void CreateAndStartTabFadeAnimation(Tab previousTab, Tab newTab)
        {
            // If tabs are the same and there is no animation running, perform sanity check and force update the state without playing animation.
            if (previousTab == newTab)
            {
                if (tabFadeSequence == null)
                {
                    foreach (UIContentTab tab in tabs)
                    {
                        tab.gameObject.SetActive(tab.Tab == newTab);
                    }
                }

                return;
            }

            tabFadeSequence?.Complete(true);

            UIContentTab previousTabObject = FindObjectForTab(previousTab);
            UIContentTab newTabObject = FindObjectForTab(newTab);
            
            newTabObject.FadeGroup.alpha = 0;
            
            Sequence newSequence = DOTween.Sequence();
            newSequence.Append(previousTabObject.FadeGroup.DOFade(0, 0.15f));
            newSequence.AppendCallback(() =>
            {
                previousTabObject.gameObject.SetActive(false);
                newTabObject.gameObject.SetActive(true);
            });
            newSequence.Append(newTabObject.FadeGroup.DOFade(1, 0.2f));
            newSequence.OnKill(() => tabFadeSequence = null);

            tabFadeSequence = newSequence;
        }
        
        private void ValidateState()
        {
            WaterQuality waterQuality = _settings.WaterQuality;
            highQualityWaterObject.SetActive(waterQuality == WaterQuality.High);
            simpleQualityWaterObject.SetActive(waterQuality == WaterQuality.Simple);
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

        public void OnTabChange(TabReference tabReference)
        {
            Tab tab = tabReference.Tab;
            CurrentTab = tab;
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            int floor = _cameraCoordinator.Current.Level;
            if (floor < 0)
            {
                groundToggle.gameObject.SetActive(false);
                cavesToggle.gameObject.SetActive(true);
                if (groundToggle.isOn)
                {
                    FindObjectForTab(Tab.Ground).gameObject.SetActive(false);
                    FindObjectForTab(Tab.Caves).gameObject.SetActive(true);
                    groundToggle.isOn = false;
                    cavesToggle.isOn = true;
                    CurrentTab = Tab.Caves;
                }
            }
            else if (floor >= 0)
            {
                groundToggle.gameObject.SetActive(true);
                cavesToggle.gameObject.SetActive(false);
                if (cavesToggle.isOn)
                {
                    FindObjectForTab(Tab.Ground).gameObject.SetActive(true);
                    FindObjectForTab(Tab.Caves).gameObject.SetActive(false);
                    groundToggle.isOn = true;
                    cavesToggle.isOn = false;
                    CurrentTab = Tab.Ground;
                }
            }
        }

        private UIContentTab FindObjectForTab(Tab tab)
        {
            foreach (UIContentTab uiTab in tabs)
            {
                if (uiTab.Tab == tab)
                {
                    return uiTab;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            tabFadeSequence?.Kill();
            
            _settings.Modified -= ValidateState;
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged -= CameraCoordinatorOnLevelChanged;
        }
    }
}