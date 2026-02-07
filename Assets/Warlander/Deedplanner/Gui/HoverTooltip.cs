using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Gui.Tooltips;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Inject] private TooltipHandler _tooltipHandler;
        
        [SerializeField] [TextArea(2, 5)] private string text = null;
        [SerializeField] private float showDelay = 0f;

        private bool _showTooltip;
        private IDisposable _delayDisposable;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showDelay <= 0)
            {
                _showTooltip = true;
                _tooltipHandler.ShowTooltipText(text);
            }
            else
            {
                _delayDisposable = Observable.Timer(TimeSpan.FromSeconds(showDelay))
                    .Subscribe(_ =>
                    {
                        _showTooltip = true;
                        _delayDisposable = null;
                    });
            }
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
            _delayDisposable?.Dispose();
            _delayDisposable = null;
        }
    }
}