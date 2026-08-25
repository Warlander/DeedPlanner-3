using System;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public interface IBridgeSegmentBarView
    {
        event Action<int> SegmentClicked;
        event Action<int> SegmentHovered;
        event Action<bool> PavingModeChanged;
        event Action<int> PavingSelected;

        void ShowBridge(Bridge bridge, bool editable, string tooltipSuffix);
        void ShowPreview(Bridge bridge, BridgePartType?[] previewSegments, string incorrectTooltip);
        void SetInvalidState(bool invalid);
        void ShowPavingPalette(BridgePavementData[] choices, int selectedIndex);
        void SetPavingSelection(int index);
        void SetPavingMode(bool pavingMode);
        void SetSupportsModeAvailable(bool available);
    }
}
