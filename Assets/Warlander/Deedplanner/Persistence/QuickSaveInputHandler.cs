using UnityEngine.InputSystem;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Inputs;
using VContainer.Unity;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Persistence
{
    public class QuickSaveInputHandler : IInitializable
    {
        private readonly DPInput _input;
        private readonly ISaveCoordinator _saveCoordinator;
        private readonly WindowCoordinator _windowCoordinator;

        public QuickSaveInputHandler(DPInput input, ISaveCoordinator saveCoordinator, WindowCoordinator windowCoordinator)
        {
            _input = input;
            _saveCoordinator = saveCoordinator;
            _windowCoordinator = windowCoordinator;
        }

        void IInitializable.Initialize()
        {
            _input.EditingControls.QuickSave.performed += QuickSaveOnPerformed;
        }

        private void QuickSaveOnPerformed(InputAction.CallbackContext obj)
        {
            if (_saveCoordinator.CanQuickSave)
            {
                _ = _saveCoordinator.QuickSaveAsync();
            }
            else
            {
                _windowCoordinator.CreateWindowExclusive(WindowNames.SaveMapWindow);
            }
        }
    }
}
