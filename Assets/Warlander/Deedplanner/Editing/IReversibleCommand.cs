namespace Warlander.Deedplanner.Editing
{
    public interface IReversibleCommand
    {
        void Execute();
        void Undo();
        void DisposeUndo();
        void DisposeRedo();
    }
}
