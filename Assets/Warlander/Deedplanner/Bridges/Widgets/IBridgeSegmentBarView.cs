using Warlander.Deedplanner.Gui.Widgets;
using System;
using Warlander.Deedplanner.Bridges;

namespace Warlander.Deedplanner.Bridges.Widgets
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
