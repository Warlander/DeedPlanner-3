using UnityEngine;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public abstract class TooltipContentBlock : MonoBehaviour, ITooltipContent
    {
        public void Show(RectTransform parent, int siblingIndex)
        {
            if (transform.parent != parent)
            {
                transform.SetParent(parent, false);
            }
            gameObject.SetActive(true);
            transform.SetSiblingIndex(siblingIndex);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
