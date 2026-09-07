using System;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveHeightEdit : IDisposable
    {
        bool SetAt(int x, int y, int value);
        void Commit();
        void Cancel();
    }
}
