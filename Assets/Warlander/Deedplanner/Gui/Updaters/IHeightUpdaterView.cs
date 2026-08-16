using System;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IHeightUpdaterView
    {
        event Action<HeightMode> ModeChanged;
        event Action<string> DragSensitivityChanged;
        event Action<bool> RespectOriginalSlopesChanged;
        event Action<string> TargetHeightChanged;

        void ShowModePanels(HeightMode mode);
        void SetDragSensitivity(string text);
        void SetRespectOriginalSlopes(bool value);
    }
}
