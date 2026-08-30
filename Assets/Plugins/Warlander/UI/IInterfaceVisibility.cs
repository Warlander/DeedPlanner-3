using System;

namespace Warlander.UI
{
    public interface IInterfaceVisibility
    {
        bool Visible { get; }
        event Action<bool> VisibilityChanged;
    }
}
