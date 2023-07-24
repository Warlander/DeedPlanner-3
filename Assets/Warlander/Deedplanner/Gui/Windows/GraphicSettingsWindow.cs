using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Settings;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class GraphicSettingsWindow : MonoBehaviour
    {
        [Inject] private DPSettings _settings;
        [Inject] private Window _window;
        
        [SerializeField] private Toggle simpleWaterToggle;
        [SerializeField] private Toggle highWaterToggle;
        [SerializeField] private Toggle ultraWaterToggle;

        [SerializeField] private TMP_Dropdown overallQualityDropdown;

        [SerializeField] private Slider guiScaleSlider;
        [SerializeField] private TMP_Text guiScaleValueText;

        [SerializeField] private Button _saveButton;

        private void Start()
        {
            string[] availableQualitySettings = QualitySettings.names;

            foreach (string qualitySetting in availableQualitySettings)
            {
                overallQualityDropdown.options.Add(new TMP_Dropdown.OptionData(qualitySetting));
            }
            
            ApplyProperties();
            
            guiScaleSlider.onValueChanged.AddListener(GuiScaleOnValueChanged);
            _saveButton.onClick.AddListener(SaveButtonOnClick);
        }

        private void SaveButtonOnClick()
        {
            SaveProperties();
            _window.Close();
        }

        private void ApplyProperties()
        {
            WaterQuality waterQuality = _settings.WaterQuality;
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
            guiScaleSlider.value = _settings.GuiScale;
            guiScaleValueText.text = Mathf.RoundToInt(guiScaleSlider.value).ToString();
        }

        private void GuiScaleOnValueChanged(float value)
        {
            guiScaleValueText.text = Mathf.RoundToInt(guiScaleSlider.value).ToString();
        }
        
        private void SaveProperties()
        {
            _settings.Modify(settings =>
            {
                if (simpleWaterToggle.isOn)
                {
                    settings.WaterQuality = WaterQuality.Simple;
                }
                else if (highWaterToggle.isOn)
                {
                    settings.WaterQuality = WaterQuality.High;
                }
                else if (ultraWaterToggle.isOn)
                {
                    settings.WaterQuality = WaterQuality.Ultra;
                }

                settings.GuiScale = Mathf.RoundToInt(guiScaleSlider.value);
            });

            QualitySettings.SetQualityLevel(overallQualityDropdown.value, true);
            LayoutManager.Instance.UpdateCanvasScale();
        }
    }
}
