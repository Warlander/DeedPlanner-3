using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public interface IOutlineCoordinator
    {
        void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority);
        void RemoveObject(DynamicModelBehaviour behaviour, int priority);
    }
}