using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using System.Linq;

namespace Warlander.Deedplanner.Gui
{

    public class LayoutManager : MonoBehaviour
    {

        public static LayoutManager Instance { get; private set; }

        private int activeWindow;
        private Layout currentLayout = Layout.Single;

        [SerializeField]
        private Toggle[] IndicatorButtons = new Toggle[4];
        [SerializeField]
        private RectTransform HorizontalBottomIndicatorHolder;
        [SerializeField]
        private RawImage[] Screens = new RawImage[4];
        [SerializeField]
        private RectTransform HorizontalBottomScreenHolder;
        [SerializeField]
        private RectTransform[] Splits = new RectTransform[5];
        [SerializeField]
        private MultiCamera[] cameras = new MultiCamera[4];
        [SerializeField]
        private ToggleGroup cameraModeGroup;
        [SerializeField]
        private Toggle[] cameraModeToggles = new Toggle[4];
        [SerializeField]
        private ToggleGroup floorGroup;
        [SerializeField]
        private Toggle[] positiveFloorToggles = new Toggle[16];
        [SerializeField]
        private Toggle[] negativeFloorToggles = new Toggle[6];

        public int ActiveWindow {
            get {
                return activeWindow;
            }
            private set {
                activeWindow = value;

                int floor = cameras[ActiveWindow].Floor;
                foreach (Toggle toggle in positiveFloorToggles)
                {
                    toggle.isOn = false;
                }
                foreach (Toggle toggle in negativeFloorToggles)
                {
                    toggle.isOn = false;
                }

                if (floor < 0)
                {
                    floor++;
                    negativeFloorToggles[floor].isOn = true;
                }
                else
                {
                    positiveFloorToggles[floor].isOn = true;
                }

                CameraMode cameraMode = cameras[ActiveWindow].CameraMode;
                foreach (Toggle toggle in cameraModeToggles)
                {
                    toggle.isOn = false;
                    if (toggle.GetComponent<CameraModeReference>().CameraMode == cameraMode)
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void OnLayoutChange(LayoutReference layoutReference)
        {
            Layout layout = layoutReference.Layout;
            currentLayout = layout;

            switch (layout)
            {
                case Layout.Single:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(false);
                    IndicatorButtons[2].gameObject.SetActive(false);
                    IndicatorButtons[3].gameObject.SetActive(false);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(false);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(false);
                    Screens[2].gameObject.SetActive(false);
                    Screens[3].gameObject.SetActive(false);
                    HorizontalBottomScreenHolder.gameObject.SetActive(false);
                    Splits[0].gameObject.SetActive(false);
                    Splits[1].gameObject.SetActive(false);
                    Splits[2].gameObject.SetActive(false);
                    Splits[3].gameObject.SetActive(false);
                    Splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0)
                    {
                        IndicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        IndicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalSplit:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(false);
                    IndicatorButtons[2].gameObject.SetActive(true);
                    IndicatorButtons[3].gameObject.SetActive(false);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(false);
                    Screens[2].gameObject.SetActive(true);
                    Screens[3].gameObject.SetActive(false);
                    HorizontalBottomScreenHolder.gameObject.SetActive(true);
                    Splits[0].gameObject.SetActive(true);
                    Splits[1].gameObject.SetActive(false);
                    Splits[2].gameObject.SetActive(false);
                    Splits[3].gameObject.SetActive(false);
                    Splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 2)
                    {
                        IndicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        IndicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.VerticalSplit:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(true);
                    IndicatorButtons[2].gameObject.SetActive(false);
                    IndicatorButtons[3].gameObject.SetActive(false);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(false);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(true);
                    Screens[2].gameObject.SetActive(false);
                    Screens[3].gameObject.SetActive(false);
                    HorizontalBottomScreenHolder.gameObject.SetActive(false);
                    Splits[0].gameObject.SetActive(false);
                    Splits[1].gameObject.SetActive(true);
                    Splits[2].gameObject.SetActive(false);
                    Splits[3].gameObject.SetActive(false);
                    Splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 1)
                    {
                        IndicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        IndicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalTop:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(false);
                    IndicatorButtons[2].gameObject.SetActive(true);
                    IndicatorButtons[3].gameObject.SetActive(true);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(false);
                    Screens[2].gameObject.SetActive(true);
                    Screens[3].gameObject.SetActive(true);
                    HorizontalBottomScreenHolder.gameObject.SetActive(true);
                    Splits[0].gameObject.SetActive(false);
                    Splits[1].gameObject.SetActive(false);
                    Splits[2].gameObject.SetActive(true);
                    Splits[3].gameObject.SetActive(false);
                    Splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 2 && ActiveWindow != 3)
                    {
                        IndicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        IndicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalBottom:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(true);
                    IndicatorButtons[2].gameObject.SetActive(true);
                    IndicatorButtons[3].gameObject.SetActive(false);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(true);
                    Screens[2].gameObject.SetActive(true);
                    Screens[3].gameObject.SetActive(false);
                    HorizontalBottomScreenHolder.gameObject.SetActive(true);
                    Splits[0].gameObject.SetActive(false);
                    Splits[1].gameObject.SetActive(false);
                    Splits[2].gameObject.SetActive(false);
                    Splits[3].gameObject.SetActive(true);
                    Splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 1 && ActiveWindow != 2)
                    {
                        IndicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        IndicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.Quad:
                    IndicatorButtons[0].gameObject.SetActive(true);
                    IndicatorButtons[1].gameObject.SetActive(true);
                    IndicatorButtons[2].gameObject.SetActive(true);
                    IndicatorButtons[3].gameObject.SetActive(true);
                    HorizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    Screens[0].gameObject.SetActive(true);
                    Screens[1].gameObject.SetActive(true);
                    Screens[2].gameObject.SetActive(true);
                    Screens[3].gameObject.SetActive(true);
                    HorizontalBottomScreenHolder.gameObject.SetActive(true);
                    Splits[0].gameObject.SetActive(false);
                    Splits[1].gameObject.SetActive(false);
                    Splits[2].gameObject.SetActive(false);
                    Splits[3].gameObject.SetActive(false);
                    Splits[4].gameObject.SetActive(true);
                    break;
            }
        }

        public void OnActiveIndicatorChange(int window)
        {
            if (ActiveWindow == window)
            {
                return;
            }

            if (IndicatorButtons[window].isOn)
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

            IndicatorButtons[ActiveWindow].isOn = false;
            IndicatorButtons[window].isOn = true;
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
            FloorReference floorReference = floorGroup.ActiveToggles().First().GetComponent<FloorReference>();
            int floor = floorReference.Floor;

            if (cameras[ActiveWindow].Floor == floor)
            {
                return;
            }

            cameras[ActiveWindow].Floor = floor;
            Debug.Log("Camera " + ActiveWindow + " floor changed to " + floor);
        }

    }

}