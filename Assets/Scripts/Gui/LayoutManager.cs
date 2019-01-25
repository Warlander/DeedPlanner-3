using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{

    public class LayoutManager : MonoBehaviour
    {

        private int activeWindow = 0;
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
                    if (activeWindow != 0)
                    {
                        IndicatorButtons[activeWindow].isOn = false;
                        activeWindow = 0;
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
                    if (activeWindow != 0 && activeWindow != 2)
                    {
                        IndicatorButtons[activeWindow].isOn = false;
                        activeWindow = 0;
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
                    if (activeWindow != 0 && activeWindow != 1)
                    {
                        IndicatorButtons[activeWindow].isOn = false;
                        activeWindow = 0;
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
                    if (activeWindow != 0 && activeWindow != 2 && activeWindow != 3)
                    {
                        IndicatorButtons[activeWindow].isOn = false;
                        activeWindow = 0;
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
                    if (activeWindow != 0 && activeWindow != 1 && activeWindow != 2)
                    {
                        IndicatorButtons[activeWindow].isOn = false;
                        activeWindow = 0;
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
            if (IndicatorButtons[window].isOn)
            {
                activeWindow = window;
            }
        }

        public void OnActiveWindowChange(int window)
        {
            IndicatorButtons[activeWindow].isOn = false;
            IndicatorButtons[window].isOn = true;
            activeWindow = window;
        }

    }

}