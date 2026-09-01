using System;
using Warlander.Deedplanner.Ui.Widgets;
using Warlander.Deedplanner.Editing;
using UnityEngine;

namespace Warlander.Deedplanner.Bridges.Widgets
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
