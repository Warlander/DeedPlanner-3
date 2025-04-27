using UnityEngine;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeTabSwapper : MonoBehaviour
    {
        [SerializeField] private BridgeTab defaultTab;
        [SerializeField] private BridgeTabRecord[] bridgeTabs;

        private void Awake()
        {
            SwitchToDefaultTab();
        }

        private void SwitchToDefaultTab()
        {
            foreach (BridgeTabRecord bridgeTab in bridgeTabs)
            {
                bridgeTab.Panel.SetActive(bridgeTab.Tab == defaultTab);
            }
        }
        
        public void SwapToTab(BridgeTab tab)
        {
            foreach (BridgeTabRecord bridgeTabRecord in bridgeTabs)
            {
                bridgeTabRecord.Panel.SetActive(bridgeTabRecord.Tab == tab);
            }
        }
    }
}