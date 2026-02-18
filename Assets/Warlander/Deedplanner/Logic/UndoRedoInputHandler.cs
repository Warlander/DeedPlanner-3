using UnityEngine.InputSystem;
using Warlander.Deedplanner.Inputs;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class UndoRedoInputHandler : IInitializable
    {
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;

        public UndoRedoInputHandler(DPInput input, MapHandler mapHandler)
        {
            _input = input;
            _mapHandler = mapHandler;
        }

        void IInitializable.Initialize()
        {
            _input.EditingControls.Undo.performed += UndoOnPerformed;
            _input.EditingControls.Redo.performed += RedoOnPerformed;
        }

        private void UndoOnPerformed(InputAction.CallbackContext obj)
        {
            _mapHandler.Map?.CommandManager.Undo();
        }

        private void RedoOnPerformed(InputAction.CallbackContext obj)
        {
            _mapHandler.Map?.CommandManager.Redo();
        }
    }
}
