using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Gui.Tooltips;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Inject] private TooltipHandler _tooltipHandler;
        
        [SerializeField] private string text = null;
        [SerializeField] private float showDelay = 0f;

        private bool _showTooltip;
        private Coroutine _delayCoroutine;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showDelay <= 0)
            {
                _showTooltip = true;
                _tooltipHandler.ShowTooltipText(text);
            }
            else
            {
                _delayCoroutine = StartCoroutine(DelayCoroutine());
            }
        }

        private IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(showDelay);
            _showTooltip = true;
            _delayCoroutine = null;
        }

        private void Update()
        {
            if (_showTooltip)
            {
                _tooltipHandler.ShowTooltipText(text);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _showTooltip = false;
            if (_delayCoroutine != null)
            {
                StopCoroutine(_delayCoroutine);
            }
        }
    }
}