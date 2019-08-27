using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class GraphicSettingsWindow : MonoBehaviour
    {

        [SerializeField] private Toggle simpleWaterToggle = null;
        [SerializeField] private Toggle highWaterToggle = null;
        [SerializeField] private Toggle ultraWaterToggle = null;

        [SerializeField] private TMP_Dropdown overallQualityDropdown = null;

        private void Awake()
        {
            string[] availableQualitySettings = QualitySettings.names;

            foreach (string qualitySetting in availableQualitySettings)
            {
                overallQualityDropdown.options.Add(new TMP_Dropdown.OptionData(qualitySetting));
            }
        }
        
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
            WaterQuality waterQuality = Properties.Instance.WaterQuality;
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
            
            overallQualityDropdown.value = QualitySettings.GetQualityLevel();
        }

        public void OnSaveButton()
        {
            SaveProperties();
            gameObject.SetActive(false);
        }

        private void SaveProperties()
        {
            if (simpleWaterToggle.isOn)
            {
                Properties.Instance.WaterQuality = WaterQuality.SIMPLE;
            }
            else if (highWaterToggle.isOn)
            {
                Properties.Instance.WaterQuality = WaterQuality.HIGH;
            }
            else if (ultraWaterToggle.isOn)
            {
                Properties.Instance.WaterQuality = WaterQuality.ULTRA;
            }

            Properties.Instance.SaveProperties();
            
            QualitySettings.SetQualityLevel(overallQualityDropdown.value, true);
        }

    }
}
