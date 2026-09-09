using System;

namespace Warlander.Deedplanner.Persistence
{
    public interface IMapSavePicker
    {
        bool Show(Action<string[]> onSuccess, Action onCancel, string suggestedName);
    }
}
