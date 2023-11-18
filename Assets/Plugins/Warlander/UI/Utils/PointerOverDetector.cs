using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.UI.Utils
{
    public class PointerOverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsPointerOver { get; private set; }

        private DateTime _pointerEnterTime;

        public TimeSpan GetPointerOverDuration()
        {
            if (IsPointerOver == false)
            {
                return TimeSpan.Zero;
            }

            return DateTime.UtcNow - _pointerEnterTime;
        }
        
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            IsPointerOver = true;
            _pointerEnterTime = DateTime.UtcNow;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            IsPointerOver = false;
        }
    }
}