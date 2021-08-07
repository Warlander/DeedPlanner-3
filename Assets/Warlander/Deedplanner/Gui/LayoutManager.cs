using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using System.Linq;
using System;
using DG.Tweening;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public class LayoutManager : MonoBehaviour
    {
        public static LayoutManager Instance { get; private set; }

        [SerializeField] private CanvasScaler mainCanvasScaler = null;
        
        [SerializeField] private Toggle[] indicatorButtons = new Toggle[4];
        [SerializeField] private RectTransform horizontalBottomIndicatorHolder = null;
        [SerializeField] private RawImage[] screens = new RawImage[4];
        [SerializeField] private RectTransform horizontalBottomScreenHolder = null;
        [SerializeField] private RectTransform[] splits = new RectTransform[5];
        [SerializeField] private MultiCamera[] cameras = new MultiCamera[4];
        [SerializeField] private ToggleGroup cameraModeGroup = null;
        [SerializeField] private Toggle[] cameraModeToggles = new Toggle[4];
        [SerializeField] private ToggleGroup floorGroup = null;
        [SerializeField] private FloorToggle[] positiveFloorToggles = new FloorToggle[16];
        [SerializeField] private FloorToggle[] negativeFloorToggles = new FloorToggle[6];

        [SerializeField] private UIContentTab[] tabs = new UIContentTab[12];
        [SerializeField] private Toggle groundToggle = null;
        [SerializeField] private Toggle cavesToggle = null;

        [SerializeField] private GameObject highQualityWaterObject = null;
        [SerializeField] private GameObject simpleQualityWaterObject = null;

        [SerializeField] private Tooltip tooltip = null;

        public event GenericEventArgs<Tab> TabChanged;

        private int activeWindow;
        private Layout currentLayout = Layout.Single;
        private Tab currentTab;

        private Sequence tabFadeSequence;
        
        public TileSelectionMode TileSelectionMode { get; set; }
        
        public MultiCamera CurrentCamera => cameras[ActiveWindow];

        public MultiCamera HoveredCamera
        {
            get
            {
                foreach (MultiCamera cam in cameras)
                {
                    if (cam.MouseOver)
                    {
                        return cam;
                    }
                }

                return null;
            }
        }

        public string TooltipText
        {
            get => tooltip.Value;
            set => tooltip.Value = value;
        }

        public int ActiveWindow {
            get => activeWindow;
            private set {
                activeWindow = value;

                int floor = cameras[ActiveWindow].Floor;
                foreach (FloorToggle toggle in positiveFloorToggles)
                {
                    toggle.Toggle.isOn = false;
                }
                foreach (FloorToggle toggle in negativeFloorToggles)
                {
                    toggle.Toggle.isOn = false;
                }

                if (floor < 0)
                {
                    floor++;
                    negativeFloorToggles[floor].Toggle.isOn = true;
                }
                else
                {
                    positiveFloorToggles[floor].Toggle.isOn = true;
                }

                CameraMode cameraMode = cameras[ActiveWindow].CameraMode;
                foreach (Toggle toggle in cameraModeToggles)
                {
                    toggle.isOn = toggle.GetComponent<CameraModeReference>().CameraMode == cameraMode;
                }
            }
        }

        public Tab CurrentTab {
            get => currentTab;
            set
            {
                CreateAndStartTabFadeAnimation(currentTab, value);
                
                currentTab = value;
                TabChanged?.Invoke(currentTab);
            }
        }

        private FloorToggle CurrentFloorToggle => floorGroup.ActiveToggles().First().GetComponent<FloorToggle>();

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

            Properties.Instance.Saved += ValidateState;
            ValidateState();
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
            WaterQuality waterQuality = Properties.Instance.WaterQuality;
            highQualityWaterObject.SetActive(waterQuality == WaterQuality.High);
            simpleQualityWaterObject.SetActive(waterQuality == WaterQuality.Simple);
        }

        public void UpdateCanvasScale()
        {
            float referenceWidth = Constants.DefaultGuiWidth;
            float referenceHeight = Constants.DefaultGuiHeight * (Properties.Instance.GuiScale * Constants.GuiScaleUnitsToRealScale);
            mainCanvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        }
        
        public void OnLayoutChange(LayoutReference layoutReference)
        {
            Layout layout = layoutReference.Layout;
            currentLayout = layout;

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
            cameras[index].gameObject.SetActive(enable);
            indicatorButtons[index].gameObject.SetActive(enable);
            screens[index].gameObject.SetActive(enable);
            
            // if screen is being toggled off, focus primary screen instead
            if (!enable && ActiveWindow == index)
            {
                indicatorButtons[ActiveWindow].isOn = false;
                ActiveWindow = 0;
                indicatorButtons[0].isOn = true;
            }
        }

        public void OnActiveIndicatorChange(int window)
        {
            if (ActiveWindow == window)
            {
                return;
            }

            if (indicatorButtons[window].isOn)
            {
                ActiveWindow = window;
                Debug.Log("Active window changed to " + ActiveWindow);
            }
        }

        public void OnActiveWindowChange(int window)
        {
            if (ActiveWindow == window)
            {
                return;
            }

            indicatorButtons[ActiveWindow].isOn = false;
            indicatorButtons[window].isOn = true;
            ActiveWindow = window;
            Debug.Log("Active window changed to " + ActiveWindow);
        }

        public void OnCameraModeChange()
        {
            CameraModeReference cameraModeReference = cameraModeGroup.ActiveToggles().First().GetComponent<CameraModeReference>();
            CameraMode cameraMode = cameraModeReference.CameraMode;

            if (cameras[ActiveWindow].CameraMode == cameraMode)
            {
                return;
            }

            cameras[ActiveWindow].CameraMode = cameraMode;
            Debug.Log("Camera " + ActiveWindow + " camera mode changed to " + cameraMode);
        }

        public void OnFloorChange()
        {
            int floor = CurrentFloorToggle.Floor;

            if (cameras[ActiveWindow].Floor == floor)
            {
                return;
            }

            cameras[ActiveWindow].Floor = floor;
            Debug.Log("Camera " + ActiveWindow + " floor changed to " + floor);
            UpdateTabs();
        }

        public void OnTabChange(TabReference tabReference)
        {
            Tab tab = tabReference.Tab;
            CurrentTab = tab;
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            int floor = CurrentFloorToggle.Floor;
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

    }

}