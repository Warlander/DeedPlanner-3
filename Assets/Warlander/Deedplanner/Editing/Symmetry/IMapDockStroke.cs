using System;
using Warlander.Deedplanner.Docks;

namespace Warlander.Deedplanner.Editing
{
    public interface IMapDockStroke : IDisposable
    {
        bool Place(DockPaintRequest request);
        void Remove(int x, int y, DockRealm realm);
        void Commit();
        void Cancel();
    }
}
