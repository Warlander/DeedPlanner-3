using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    public class ScreenshotInputListener : IInitializable, IDisposable
    {
        private readonly DPInput _input;
        private readonly CurrentViewScreenshotCapture _currentViewCapture;
        private readonly BackbufferScreenshotCapture _backbufferCapture;
        private readonly ScreenshotSaver _saver;

        public ScreenshotInputListener(DPInput input, CurrentViewScreenshotCapture currentViewCapture,
            BackbufferScreenshotCapture backbufferCapture, ScreenshotSaver saver)
        {
            _input = input;
            _currentViewCapture = currentViewCapture;
            _backbufferCapture = backbufferCapture;
            _saver = saver;
        }

        void IInitializable.Initialize()
        {
            _input.EditingControls.TakeScreenshot.performed += OnTakeScreenshot;
            _input.EditingControls.TakeScreenshotWithUI.performed += OnTakeScreenshotWithUI;
        }

        public void Dispose()
        {
            _input.EditingControls.TakeScreenshot.performed -= OnTakeScreenshot;
            _input.EditingControls.TakeScreenshotWithUI.performed -= OnTakeScreenshotWithUI;
        }

        private void OnTakeScreenshot(InputAction.CallbackContext context)
        {
            CaptureAndSave(_currentViewCapture);
        }

        private void OnTakeScreenshotWithUI(InputAction.CallbackContext context)
        {
            CaptureAndSave(_backbufferCapture);
        }

        private async void CaptureAndSave(IScreenshotCapture capture)
        {
            Texture2D texture = await capture.CaptureAsync();
            if (texture != null)
            {
                _saver.Save(texture);
            }
        }
    }
}
