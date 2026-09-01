using System;
using Warlander.Deedplanner.Data.Roofs;

namespace Warlander.Deedplanner.Editing
{
    public interface IRoofUpdaterView
    {
        event Action<RoofData> RoofSelected;
        void AddRoofEntry(RoofData data);
        void PushSelection();
    }
}
