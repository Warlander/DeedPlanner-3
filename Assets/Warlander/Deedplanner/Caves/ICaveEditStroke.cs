using System;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveEditStroke : IDisposable
    {
        bool ApplyAt(int x, int y);
        void Commit();
        void Cancel();
    }
}
