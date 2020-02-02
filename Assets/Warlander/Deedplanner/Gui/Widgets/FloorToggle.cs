using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class FloorToggle : MonoBehaviour
    {
        [SerializeField] private int floor = 0;

        private Toggle toggle;
        
        public Toggle Toggle => toggle;
        public int Floor => floor;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }
    }
}