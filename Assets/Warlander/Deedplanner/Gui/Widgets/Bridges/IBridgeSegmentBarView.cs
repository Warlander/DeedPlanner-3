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
        event Action ApplyToAllClicked;

        void ShowBridge(Bridge bridge, bool editable, string tooltipSuffix);
        void ShowPreview(Bridge bridge, BridgePartType?[] previewSegments, string incorrectTooltip);
        void SetInvalidState(bool invalid);
        void SetPavingChoices(BridgePavementData[] choices, int selectedIndex);
        void SetPavingMode(bool pavingMode);
        void SetModeSwitchAvailable(bool available);
        void ShowPavements(Bridge bridge, BridgePavementData[] pavements);
    }
}
