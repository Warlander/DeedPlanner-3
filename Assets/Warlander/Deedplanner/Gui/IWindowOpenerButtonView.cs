using System;

namespace Warlander.Deedplanner.Gui
{
    public interface IWindowOpenerButtonView
    {
        event Action<WindowOpenRequest> WindowOpenRequested;
    }
}
