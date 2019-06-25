using System.Collections.Generic;

namespace Warlander.Deedplanner.Utils
{
    public class CommandManager
    {

        private readonly LinkedList<IUndoCommand> undoList = new LinkedList<IUndoCommand>();
        private readonly LinkedList<IUndoCommand> redoList = new LinkedList<IUndoCommand>();

        private readonly Stack<IUndoCommand> currentActionStack = new Stack<IUndoCommand>();

        public int MaxUndoCount { get; }
        
        public CommandManager(int maxUndoCount)
        {
            MaxUndoCount = maxUndoCount;
        }
        
        public void Undo()
        {
            if (undoList.Count == 0)
            {
                return;
            }

            IUndoCommand undoCommand = undoList.First.Value;
            undoList.RemoveFirst();
            undoCommand.Undo();
            redoList.AddFirst(undoCommand);
        }

        public void Redo()
        {
            if (redoList.Count == 0)
            {
                return;
            }
            
            IUndoCommand undoCommand = redoList.First.Value;
            redoList.RemoveFirst();
            undoCommand.Execute();
            undoList.AddFirst(undoCommand);
        }
        
        public void AddToStack(IUndoCommand undoCommand)
        {
            undoList.AddFirst(undoCommand);
            foreach (IUndoCommand command in redoList)
            {
                command.DisposeRedo();
            }
            redoList.Clear();

            if (undoList.Count > MaxUndoCount)
            {
                IUndoCommand removedCommand = undoList.Last.Value;
                removedCommand.DisposeUndo();
                undoList.RemoveLast();
            }
        }
        
        public void AddToActionAndExecute(IUndoCommand undoCommand)
        {
            currentActionStack.Push(undoCommand);
            undoCommand.Execute();
        }

        public void FinishAction()
        {
            if (currentActionStack.Count == 0)
            {
                return;
            }
            
            AddToStack(new UndoCommandAction(currentActionStack.ToArray()));
            currentActionStack.Clear();
        }

        public void UndoAction()
        {
            foreach (IUndoCommand command in currentActionStack)
            {
                command.Undo();
            }
            currentActionStack.Clear();
        }

        private struct UndoCommandAction : IUndoCommand
        {

            private readonly IUndoCommand[] containedUndoCommands;

            public UndoCommandAction(IUndoCommand[] undoCommands)
            {
                containedUndoCommands = undoCommands;
            }
            
            public void Execute()
            {
                foreach (IUndoCommand containedCommand in containedUndoCommands)
                {
                    containedCommand.Execute();
                }
            }

            public void Undo()
            {
                foreach (IUndoCommand containedCommand in containedUndoCommands)
                {
                    containedCommand.Undo();
                }
            }

            public void DisposeUndo()
            {
                foreach (IUndoCommand containedUndoCommand in containedUndoCommands)
                {
                    containedUndoCommand.DisposeUndo();
                }
            }

            public void DisposeRedo()
            {
                foreach (IUndoCommand containedUndoCommand in containedUndoCommands)
                {
                    containedUndoCommand.DisposeRedo();
                }
            }
        }
        
    }

}