using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class FloorToggle : MonoBehaviour
    {
        [SerializeField] private int floor;

        private Toggle toggle;
        
        public Toggle Toggle => toggle;
        public int Floor => floor;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }
    }
}