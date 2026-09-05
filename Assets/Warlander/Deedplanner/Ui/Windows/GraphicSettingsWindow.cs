using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Warlander.Deedplanner.Settings;
using Warlander.UI.Windows;
using QualityLevel = Warlander.Deedplanner.Settings.QualityLevel;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class GraphicSettingsWindow : MonoBehaviour
    {
        [Inject] private GraphicsOptions _graphics;
        [Inject] private UiSettings _ui;
        private Window _window;
        
        [SerializeField] private Toggle simpleWaterToggle;
        [SerializeField] private Toggle highWaterToggle;
        [SerializeField] private Toggle ultraWaterToggle;

        [SerializeField] private TMP_Dropdown overallQualityDropdown;

        [SerializeField] private Slider guiScaleSlider;
        [SerializeField] private TMP_Text guiScaleValueText;

        [SerializeField] private Toggle compassToggle; 

        [SerializeField] private Button _saveButton;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

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
            WaterQuality waterQuality = _graphics.WaterQuality;
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
            
            bool compassVisibility = _ui.CompassVisibility;
            if (compassVisibility)
            {
                compassToggle.isOn = true;
            }
            else
            {
                compassToggle.isOn = false;
            }
            
            overallQualityDropdown.value = (int) _graphics.QualityLevel;            guiScaleSlider.value = _ui.GuiScale;
            guiScaleValueText.text = Mathf.RoundToInt(guiScaleSlider.value).ToString();
        }

        private void GuiScaleOnValueChanged(float value)
        {
            guiScaleValueText.text = Mathf.RoundToInt(guiScaleSlider.value).ToString();
        }
        
        private void SaveProperties()
        {
            if (simpleWaterToggle.isOn)
            {
                _graphics.WaterQuality = WaterQuality.Simple;
            }
            else if (highWaterToggle.isOn)
            {
                _graphics.WaterQuality = WaterQuality.High;
            }
            else if (ultraWaterToggle.isOn)
            {
                _graphics.WaterQuality = WaterQuality.Ultra;
            }

            _ui.GuiScale = Mathf.RoundToInt(guiScaleSlider.value);
            _ui.CompassVisibility = compassToggle.isOn;
            _graphics.QualityLevel = (QualityLevel) overallQualityDropdown.value;
        }
    }
}
