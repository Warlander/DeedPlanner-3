using System;

namespace Warlander.Deedplanner.Editing
{
    public interface IMirrorUpdaterView
    {
        event Action PickVerticalClicked;
        event Action PickHorizontalClicked;
        event Action<bool> VerticalEnabledChanged;
        event Action<bool> HorizontalEnabledChanged;
        event Action ClearClicked;
        event Action ConfigureShortcutsClicked;

        void SetState(bool hasVerticalAxis, bool verticalEnabled, string verticalPosition,
            bool hasHorizontalAxis, bool horizontalEnabled, string horizontalPosition);
        void SetPickMode(bool pickingVertical, bool pickingHorizontal);
        void SetShortcutLabels(string labels);
    }
}
