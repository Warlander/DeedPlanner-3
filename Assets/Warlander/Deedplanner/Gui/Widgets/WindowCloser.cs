using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Warlander.Deedplanner.Gui.Widgets
{

    public class WindowCloser : MonoBehaviour, IPointerDownHandler
    {

        public Window windowToClose;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (windowToClose)
            {
                windowToClose.HideWindow();
            }
        }

    }

}