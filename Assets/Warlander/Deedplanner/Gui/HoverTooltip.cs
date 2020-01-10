using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui
{
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string text;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            LayoutManager.Instance.TooltipText = text;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            LayoutManager.Instance.TooltipText = "";
        }
    }
}