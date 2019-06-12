using System.Collections.Generic;

namespace Warlander.Deedplanner.Utils
{
    public class CommandManager
    {

        private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> redoStack = new Stack<ICommand>();

        private readonly Stack<ICommand> currentActionStack = new Stack<ICommand>();

        public void Undo()
        {
            if (undoStack.Count == 0)
            {
                return;
            }
            
            ICommand command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }

        public void Redo()
        {
            if (redoStack.Count == 0)
            {
                return;
            }
            
            ICommand command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
        }
        
        public void AddToStack(ICommand command)
        {
            undoStack.Push(command);
            redoStack.Clear();
        }
        
        public void AddToActionAndExecute(ICommand command)
        {
            currentActionStack.Push(command);
            command.Execute();
        }

        public void FinishAction()
        {
            if (currentActionStack.Count == 0)
            {
                return;
            }
            
            AddToStack(new CommandAction(currentActionStack.ToArray()));
            currentActionStack.Clear();
        }

        public void UndoAction()
        {
            foreach (ICommand command in currentActionStack)
            {
                command.Undo();
            }
            currentActionStack.Clear();
        }

        private struct CommandAction : ICommand
        {

            private readonly ICommand[] containedCommands;

            public CommandAction(ICommand[] commands)
            {
                containedCommands = commands;
            }
            
            public void Execute()
            {
                foreach (ICommand containedCommand in containedCommands)
                {
                    containedCommand.Execute();
                }
            }

            public void Undo()
            {
                foreach (ICommand containedCommand in containedCommands)
                {
                    containedCommand.Undo();
                }
            }
        }
        
    }

}