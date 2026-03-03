using System.Collections.Generic;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Graphics.Outline;

namespace Warlander.Deedplanner.Logic
{
    public interface IOutlineCoordinator
    {
        void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority);
        void RemoveObject(DynamicModelBehaviour behaviour, int priority);

        bool HasOutlinedObjects { get; }
        List<OutlineEntry> GetOutlinedObjectsSnapshot();
    }
}
