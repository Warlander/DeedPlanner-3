using System;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    [Serializable]
    public class BridgeTabRecord
    {
        [SerializeField] private BridgeTab tab;
        [SerializeField] private GameObject panel;
        
        public BridgeTab Tab => tab;
        public GameObject Panel => panel;
    }
}