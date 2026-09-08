using System;

namespace Warlander.Deedplanner.Editing
{
    public interface IHeightUpdaterView
    {
        event Action<HeightMode> ModeChanged;
        event Action<string> DragSensitivityChanged;
        event Action<bool> RespectOriginalSlopesChanged;
        event Action<string> TargetHeightChanged;
        event Action<CaveHeightMode> CaveHeightModeChanged;
        event Action<bool> PreserveCaveCeilingChanged;

        void ShowModePanels(HeightMode mode);
        void SetDragSensitivity(string text);
        void SetRespectOriginalSlopes(bool value);
        void SetTargetHeight(string text);
        void ShowCaveHeightModes(bool visible);
        void SetPreserveCaveCeiling(bool value);
        void ShowPreserveCaveCeiling(bool visible);
    }
}
