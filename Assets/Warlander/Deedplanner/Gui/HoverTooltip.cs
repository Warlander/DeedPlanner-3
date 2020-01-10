using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui
{
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string text = null;
        [SerializeField] private float showDelay = 0f;
        
        private Coroutine delayCoroutine;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showDelay <= 0)
            {
                LayoutManager.Instance.TooltipText = text;
            }
            else
            {
                delayCoroutine = StartCoroutine(DelayCoroutine());
            }
        }

        private IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(showDelay);
            LayoutManager.Instance.TooltipText = text;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            LayoutManager.Instance.TooltipText = "";
            StopCoroutine(delayCoroutine);
        }
    }
}