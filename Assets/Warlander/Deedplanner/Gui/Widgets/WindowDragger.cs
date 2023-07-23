using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.ExtensionUtils;

namespace Warlander.Deedplanner.Gui.Widgets
{

    public class WindowDragger : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Canvas _referenceCanvas;
        [SerializeField] private CanvasScaler _referenceCanvasScaler;
        [SerializeField] private RectTransform _draggedWindow;

        public void OnDrag(PointerEventData eventData)
        {
            if (_draggedWindow == null)
            {
                return;
            }
            
            Vector2 renderDisplaySize = _referenceCanvas.renderingDisplaySize;
            Vector2 canvasSize = _referenceCanvasScaler.referenceResolution;
            Vector2 scaleFactor = canvasSize / renderDisplaySize;

            Vector2 scaledDelta = eventData.delta * scaleFactor.Max();
            _draggedWindow.anchoredPosition += scaledDelta;
        }
    }

}