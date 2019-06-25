namespace Warlander.Deedplanner.Utils
{
    public interface IUndoCommand
    {
        void Execute();
        void Undo();
        void DisposeUndo();
        void DisposeRedo();
    }
}