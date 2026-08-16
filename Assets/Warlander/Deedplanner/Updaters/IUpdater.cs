using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public interface IUpdater
    {
        Tab TargetTab { get; }
        void Initialize();
        void Enable();
        void Disable();
        void Tick();
    }
}
