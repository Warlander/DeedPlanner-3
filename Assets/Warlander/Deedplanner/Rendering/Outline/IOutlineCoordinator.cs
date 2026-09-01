using System.Collections.Generic;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Rendering.Outline
{
    public interface IOutlineCoordinator
    {
        void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority);
        void RemoveObject(DynamicModelBehaviour behaviour, int priority);

        bool HasOutlinedObjects { get; }
        bool RenderingSuspended { get; set; }
        List<OutlineEntry> GetOutlinedObjectsSnapshot();
    }
}
