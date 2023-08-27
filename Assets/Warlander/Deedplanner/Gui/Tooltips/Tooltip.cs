using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Warlander.Deedplanner.Inputs;
using Warlander.ExtensionUtils;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class Tooltip : MonoBehaviour
    {
        [Inject] private DPInput _input;
        
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private RectTransform _referenceCanvasTransform;
        [SerializeField] private RectTransform _transformToMove;
        [SerializeField] private float animationSpeed = 10f;
        [SerializeField] private Vector2 _cursorCorrection = new Vector2(0, -20);

        private Vector2 _cursorCorrectionToUse;
        
        public string Value
        {
            get => text.text;
            set
            {
                bool empty = string.IsNullOrEmpty(value);

                if (empty)
                {
                    canvasGroup.DOKill();
                    canvasGroup.DOFade(0, animationSpeed).SetEase(Ease.Linear).SetSpeedBased()
                        .OnComplete(() => gameObject.SetActive(false));
                }
                else
                {
                    canvasGroup.DOKill();
                    canvasGroup.DOFade(1, animationSpeed).SetEase(Ease.Linear).SetSpeedBased();
                    gameObject.SetActive(true);
                    text.text = value;
                }
            }
        }

        private void Awake()
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
        
        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 focusPos = _input.MapInputShared.FocusPosition.ReadValue<Vector2>();
            
            Rect referenceCanvasRect = _referenceCanvasTransform.rect;
            float widthRatio = _canvasScaler.referenceResolution.y / _canvasScaler.referenceResolution.x;
            Vector2 tooltipSize = _transformToMove.sizeDelta;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_referenceCanvasTransform,
                focusPos.AddX(tooltipSize.x * widthRatio), null, out Vector2 tooltipRightmostPoint);
            bool rightEdgeWithinBounds = referenceCanvasRect.Contains(tooltipRightmostPoint);
            int pivotX = rightEdgeWithinBounds ? 0 : 1;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_referenceCanvasTransform,
                focusPos.AddY(-tooltipSize.y), null, out Vector2 tooltipBottommostPoint);
            bool bottomEdgeWithinBounds = referenceCanvasRect.Contains(tooltipBottommostPoint);
            int pivotY = bottomEdgeWithinBounds ? 1 : 0;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _referenceCanvasTransform, focusPos, null, out Vector2 localPos);
            
            bool isPointerOnScreen = referenceCanvasRect.Contains(localPos);
            
            // Don't update pivot and connection if pointer goes off-screen - this will cause sudden tooltip shift otherwise.
            if (isPointerOnScreen)
            {
                _transformToMove.pivot = new Vector2(pivotX, pivotY);
                
                Vector2 finalCursorCorrection = _cursorCorrection;
                if (rightEdgeWithinBounds == false)
                {
                    finalCursorCorrection = finalCursorCorrection.SetX(-finalCursorCorrection.x);
                }
                if (bottomEdgeWithinBounds == false)
                {
                    finalCursorCorrection = finalCursorCorrection.SetY(-finalCursorCorrection.y);
                }

                _cursorCorrectionToUse = finalCursorCorrection;
            }

            _transformToMove.localPosition = localPos + _cursorCorrectionToUse;
        }

        private void OnDestroy()
        {
            canvasGroup.DOKill();
        }
    }
}