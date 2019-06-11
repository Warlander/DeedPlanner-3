namespace Warlander.Deedplanner.Utils
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}