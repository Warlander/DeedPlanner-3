using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class GraphicsWindow : MonoBehaviour
    {

        [SerializeField]
        private Toggle simpleWaterToggle = null;
        [SerializeField]
        private Toggle highWaterToggle = null;
        [SerializeField]
        private Toggle ultraWaterToggle = null;

        private void OnEnable()
        {
            ResetState();
            ApplyProperties();
        }

        private void ResetState()
        {
            simpleWaterToggle.isOn = false;
            highWaterToggle.isOn = false;
            ultraWaterToggle.isOn = false;
        }

        private void ApplyProperties()
        {
            WaterQuality waterQuality = Properties.WaterQuality;
            switch (waterQuality)
            {
                case WaterQuality.SIMPLE:
                    simpleWaterToggle.isOn = true;
                    break;
                case WaterQuality.HIGH:
                    highWaterToggle.isOn = true;
                    break;
                case WaterQuality.ULTRA:
                    ultraWaterToggle.isOn = true;
                    break;
            }
        }

        public void OnSaveButton()
        {
            gameObject.SetActive(false);
            SaveProperties();
        }

        private void SaveProperties()
        {
            if (simpleWaterToggle.isOn)
            {
                Properties.WaterQuality = WaterQuality.SIMPLE;
            }
            else if (highWaterToggle.isOn)
            {
                Properties.WaterQuality = WaterQuality.HIGH;
            }
            else if (ultraWaterToggle.isOn)
            {
                Properties.WaterQuality = WaterQuality.ULTRA;
            }

            Properties.SaveProperties();
        }

    }
}
