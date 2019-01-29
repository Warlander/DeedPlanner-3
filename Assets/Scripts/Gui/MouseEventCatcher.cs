using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui
{
    [System.Serializable]
    public class PointerEvent : UnityEvent<PointerEventData> { }

    public class MouseEventCatcher : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {

        public PointerEvent OnBeginDragEvent = new PointerEvent();
        public PointerEvent OnDragEvent = new PointerEvent();
        public PointerEvent OnEndDragEvent = new PointerEvent();
        public PointerEvent OnPointerDownEvent = new PointerEvent();

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

    }
}