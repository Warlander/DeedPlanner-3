using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui.Widgets
{

    public class WindowDragger : MonoBehaviour, IDragHandler
    {

        public RectTransform draggedWindow;

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedWindow)
            {
                draggedWindow.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0);
            }
        }
    }

}