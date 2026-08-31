using System;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using VContainer.Unity;

namespace Warlander.Deedplanner.Screenshots
{
    public class ScreenshotInputListener : IInitializable, IDisposable
    {
        private readonly DPInput _input;
        private readonly IScreenshotFacade _screenshotFacade;

        public ScreenshotInputListener(DPInput input, IScreenshotFacade screenshotFacade)
        {
            _input = input;
            _screenshotFacade = screenshotFacade;
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

        private async void OnTakeScreenshot(InputAction.CallbackContext context)
        {
            await _screenshotFacade.CaptureAndSaveCurrentViewAsync();
        }

        private async void OnTakeScreenshotWithUI(InputAction.CallbackContext context)
        {
            await _screenshotFacade.CaptureAndSaveWithUIAsync();
        }
    }
}
