using System;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public interface IBridgeSegmentBarView
    {
        event Action<int> SegmentClicked;

        void ShowBridge(Bridge bridge, bool editable, string tooltipSuffix);
        void ShowPreview(Bridge bridge, BridgePartType?[] previewSegments, string incorrectTooltip);
        void SetInvalidState(bool invalid);
    }
}
