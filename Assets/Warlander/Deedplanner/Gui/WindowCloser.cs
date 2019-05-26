using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui
{

    public class WindowCloser : MonoBehaviour, IPointerDownHandler
    {

        public GameObject windowToClose;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (windowToClose)
            {
                windowToClose.SetActive(false);
            }
        }

    }

}