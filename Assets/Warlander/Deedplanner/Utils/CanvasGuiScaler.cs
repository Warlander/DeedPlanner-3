using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Utils
{
    public class CanvasGuiScaler : MonoBehaviour
    {
        [Inject] private DPSettings _settings;
        
        [SerializeField] private CanvasScaler _canvasScaler;

        private void Start()
        {
            ApplyScale();
            
            _settings.Modified += DpSettingsOnModified;
        }

        private void DpSettingsOnModified()
        {
            ApplyScale();
        }

        private void ApplyScale()
        {
            float referenceWidth = Constants.DefaultGuiWidth;
            float referenceHeight = Constants.DefaultGuiHeight;
            float scaleFactor = _settings.GuiScale * Constants.GuiScaleUnitsToRealScale;
            _canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight) * scaleFactor;
        }
        
        private void OnDestroy()
        {
            _settings.Modified -= DpSettingsOnModified;
        }
    }
}