using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui
{
    [System.Serializable]
    public class PointerEvent : UnityEvent<PointerEventData> { }

    public class MouseEventCatcher : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {

        public PointerEvent OnBeginDragEvent = new PointerEvent();
        public PointerEvent OnDragEvent = new PointerEvent();
        public PointerEvent OnEndDragEvent = new PointerEvent();
        public PointerEvent OnPointerDownEvent = new PointerEvent();
        public PointerEvent OnPointerEnterEvent = new PointerEvent();
        public PointerEvent OnPointerExitEvent = new PointerEvent();

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragEvent.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragEvent.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragEvent.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownEvent.Invoke(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterEvent.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent.Invoke(eventData);
        }
    }
}