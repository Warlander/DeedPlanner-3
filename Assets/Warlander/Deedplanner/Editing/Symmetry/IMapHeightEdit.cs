using System;

namespace Warlander.Deedplanner.Editing
{
    public interface IMapHeightEdit : IDisposable
    {
        void BeginPreview();
        void SetAt(int x, int y, int value);
        void ApplyPreview(Action<int, int> onChanged = null);
        void Commit();
        void Cancel();
    }
}
