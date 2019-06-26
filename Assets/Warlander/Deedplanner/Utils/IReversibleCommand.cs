namespace Warlander.Deedplanner.Utils
{
    public interface IReversibleCommand
    {
        void Execute();
        void Undo();
        void DisposeUndo();
        void DisposeRedo();
    }
}