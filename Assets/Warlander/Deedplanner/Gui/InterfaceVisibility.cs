using System;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Warlander.Deedplanner.Inputs;
using Warlander.UI;

namespace Warlander.Deedplanner.Gui
{
    public class InterfaceVisibility : IInterfaceVisibility, IInitializable, IDisposable
    {
        private readonly DPInput _input;

        public bool Visible { get; private set; } = true;
        public event Action<bool> VisibilityChanged;

        public InterfaceVisibility(DPInput input)
        {
            _input = input;
        }

        public void Initialize()
        {
            _input.EditingControls.ToggleUI.started += ToggleStarted;
        }

        private void ToggleStarted(InputAction.CallbackContext context)
        {
            Visible = !Visible;
            VisibilityChanged?.Invoke(Visible);
        }

        public void Dispose()
        {
            _input.EditingControls.ToggleUI.started -= ToggleStarted;
        }
    }
}
