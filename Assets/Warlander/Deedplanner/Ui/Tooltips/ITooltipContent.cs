using UnityEngine;

namespace Warlander.Deedplanner.Ui.Tooltips
{
    /// <summary>
    /// Content that can be scheduled on the tooltip via TooltipHandler.ShowTooltipContent.
    /// Implementations are persistent objects owning a view block - producers update their data
    /// and schedule them every frame while hovered; blocks are laid out top to bottom by priority.
    /// </summary>
    public interface ITooltipContent
    {
        void Show(RectTransform parent, int siblingIndex);
        void Hide();
    }
}
