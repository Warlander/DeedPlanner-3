using System;

namespace Warlander.Deedplanner.Ui
{
    public interface IWindowOpenerButtonView
    {
        event Action<WindowOpenRequest> WindowOpenRequested;
    }
}
