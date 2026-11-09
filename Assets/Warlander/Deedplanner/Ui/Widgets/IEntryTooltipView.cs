using System;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public interface IEntryTooltipView
    {
        event Action<string> TooltipRequested;
    }
}
