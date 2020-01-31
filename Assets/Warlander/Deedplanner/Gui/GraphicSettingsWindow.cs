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

        [SerializeField] private Slider guiScaleSlider = null;
        [SerializeField] private TMP_Text guiScaleValueText = null;

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
                case WaterQuality.Simple:
                    simpleWaterToggle.isOn = true;
                    break;
                case WaterQuality.High:
                    highWaterToggle.isOn = true;
                    break;
                case WaterQuality.Ultra:
                    ultraWaterToggle.isOn = true;
                    break;
            }
            
            overallQualityDropdown.value = QualitySettings.GetQualityLevel();
            guiScaleSlider.value = Properties.Instance.GuiScale;
        }

        public void OnGuiScaleChanged()
        {
            guiScaleValueText.text = Mathf.RoundToInt(guiScaleSlider.value).ToString();
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
                Properties.Instance.WaterQuality = WaterQuality.Simple;
            }
            else if (highWaterToggle.isOn)
            {
                Properties.Instance.WaterQuality = WaterQuality.High;
            }
            else if (ultraWaterToggle.isOn)
            {
                Properties.Instance.WaterQuality = WaterQuality.Ultra;
            }

            Properties.Instance.GuiScale = Mathf.RoundToInt(guiScaleSlider.value);

            Properties.Instance.SaveProperties();
            
            QualitySettings.SetQualityLevel(overallQualityDropdown.value, true);
            LayoutManager.Instance.UpdateCanvasScale();
        }
    }
}
