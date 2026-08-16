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
        private readonly CurrentViewScreenshotCapture _capture;
        private readonly ScreenshotSaver _saver;

        public ScreenshotInputListener(DPInput input, CurrentViewScreenshotCapture capture, ScreenshotSaver saver)
        {
            _input = input;
            _capture = capture;
            _saver = saver;
        }

        void IInitializable.Initialize()
        {
            _input.EditingControls.TakeScreenshot.performed += OnTakeScreenshot;
        }

        public void Dispose()
        {
            _input.EditingControls.TakeScreenshot.performed -= OnTakeScreenshot;
        }

        private void OnTakeScreenshot(InputAction.CallbackContext context)
        {
            Texture2D texture = _capture.CaptureCurrentView();
            if (texture != null)
            {
                _saver.Save(texture);
            }
        }
    }
}
