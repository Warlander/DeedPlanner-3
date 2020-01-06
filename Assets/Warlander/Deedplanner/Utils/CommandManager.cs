using System.Collections.Generic;

namespace Warlander.Deedplanner.Utils
{
    public class CommandManager
    {

        private readonly LinkedList<IReversibleCommand> undoList = new LinkedList<IReversibleCommand>();
        private readonly LinkedList<IReversibleCommand> redoList = new LinkedList<IReversibleCommand>();

        private readonly Stack<IReversibleCommand> currentActionStack = new Stack<IReversibleCommand>();

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

            IReversibleCommand reversibleCommand = undoList.First.Value;
            undoList.RemoveFirst();
            reversibleCommand.Undo();
            redoList.AddFirst(reversibleCommand);
        }

        public void Redo()
        {
            if (redoList.Count == 0)
            {
                return;
            }
            
            IReversibleCommand reversibleCommand = redoList.First.Value;
            redoList.RemoveFirst();
            reversibleCommand.Execute();
            undoList.AddFirst(reversibleCommand);
        }
        
        public void AddToStack(IReversibleCommand reversibleCommand)
        {
            undoList.AddFirst(reversibleCommand);
            foreach (IReversibleCommand command in redoList)
            {
                command.DisposeRedo();
            }
            redoList.Clear();

            if (undoList.Count > MaxUndoCount)
            {
                IReversibleCommand removedCommand = undoList.Last.Value;
                undoList.RemoveLast();
                removedCommand.DisposeUndo();
            }
        }
        
        public void AddToActionAndExecute(IReversibleCommand reversibleCommand)
        {
            currentActionStack.Push(reversibleCommand);
            reversibleCommand.Execute();
        }

        public void FinishAction()
        {
            if (currentActionStack.Count == 0)
            {
                return;
            }
            
            AddToStack(new ReversibleCommandAction(currentActionStack.ToArray()));
            currentActionStack.Clear();
        }

        public void UndoAction()
        {
            foreach (IReversibleCommand command in currentActionStack)
            {
                command.Undo();
                command.DisposeRedo();
            }
            currentActionStack.Clear();
        }

        public void ForgetAction()
        {
            currentActionStack.Clear();
        }

        private class ReversibleCommandAction : IReversibleCommand
        {

            private readonly IReversibleCommand[] containedReversibleCommands;

            public ReversibleCommandAction(IReversibleCommand[] reversibleCommands)
            {
                containedReversibleCommands = reversibleCommands;
            }
            
            public void Execute()
            {
                foreach (IReversibleCommand containedCommand in containedReversibleCommands)
                {
                    containedCommand.Execute();
                }
            }

            public void Undo()
            {
                foreach (IReversibleCommand containedCommand in containedReversibleCommands)
                {
                    containedCommand.Undo();
                }
            }

            public void DisposeUndo()
            {
                foreach (IReversibleCommand containedUndoCommand in containedReversibleCommands)
                {
                    containedUndoCommand.DisposeUndo();
                }
            }

            public void DisposeRedo()
            {
                foreach (IReversibleCommand containedUndoCommand in containedReversibleCommands)
                {
                    containedUndoCommand.DisposeRedo();
                }
            }
        }
        
    }

}