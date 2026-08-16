using System;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public interface IBridgeEditingView
    {
        event Action DeleteClicked;
        event Action CancelClicked;
        event Action BecameActive;
        event Action BecameInactive;

        bool IsActive { get; }

        void SetDeleteButtonVisible(bool visible);
        void SetCancelButtonVisible(bool visible);
    }
}
