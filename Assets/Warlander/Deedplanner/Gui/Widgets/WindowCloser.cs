using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui.Widgets
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