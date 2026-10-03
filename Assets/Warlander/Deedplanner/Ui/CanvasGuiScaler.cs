using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using VContainer;

namespace Warlander.Deedplanner.Ui
{
    public class CanvasGuiScaler : MonoBehaviour
    {
        [Inject] private UiSettings _settings;
        [Inject] private DPInput _input;

        [SerializeField] private CanvasScaler _canvasScaler;
        private bool _scalePending;

        private void Start()
        {
            ApplyScale();

            _settings.GuiScaleChanged += OnScaleChanged;
        }

        private void OnScaleChanged()
        {
            _scalePending = true;
            if (!IsPointerPressed())
            {
                ApplyScale();
            }
        }

        private void Update()
        {
            if (_scalePending && !IsPointerPressed())
            {
                ApplyScale();
            }
        }

        private bool IsPointerPressed()
        {
            foreach (var control in _input.UI.Click.controls)
            {
                if (control.IsPressed())
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyScale()
        {
            float referenceWidth = Constants.DefaultGuiWidth;
            float referenceHeight = Constants.DefaultGuiHeight;
            float scaleFactor = _settings.GuiScale * Constants.GuiScaleUnitsToRealScale;
            _canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight) * scaleFactor;
            _scalePending = false;
        }
        
        private void OnDestroy()
        {
            _settings.GuiScaleChanged -= OnScaleChanged;
        }
    }
}
