using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Warlander.Deedplanner.Inputs;
using Warlander.ExtensionUtils;
using Warlander.UI.Windows;
using VContainer;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class Tooltip : MonoBehaviour
    {
        [Inject] private DPInput _input;

        [SerializeField] private TooltipTextBlock textTemplate;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private RectTransform _referenceCanvasTransform;
        [SerializeField] private RectTransform _transformToMove;
        [SerializeField] private float animationSpeed = 10f;
        [SerializeField] private Vector2 _cursorCorrection = new Vector2(0, -20);

        private Vector2 _cursorCorrectionToUse;
        private readonly List<TooltipTextBlock> _textBlocks = new List<TooltipTextBlock>();
        private readonly List<ITooltipContent> _shownContents = new List<ITooltipContent>();
        private int _textBlockCursor;

        public void SetContents(IReadOnlyList<ITooltipContent> contents)
        {
            _textBlockCursor = 0;
            bool empty = contents == null || contents.Count == 0;

            if (empty)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0, animationSpeed).SetEase(Ease.Linear).SetSpeedBased()
                    .OnComplete(() => gameObject.SetActive(false));
                return;
            }

            canvasGroup.DOKill();
            canvasGroup.DOFade(1, animationSpeed).SetEase(Ease.Linear).SetSpeedBased();
            gameObject.SetActive(true);

            int siblingIndex = 0;
            foreach (ITooltipContent content in contents)
            {
                content.Show(_transformToMove, siblingIndex++);
            }

            for (int i = 0; i < _shownContents.Count; i++)
            {
                if (!contents.Contains(_shownContents[i]))
                {
                    _shownContents[i].Hide();
                }
            }
            _shownContents.Clear();
            _shownContents.AddRange(contents);
        }

        public T GetContent<T>() where T : TooltipContentBlock
        {
            return _transformToMove.GetComponentInChildren<T>(true);
        }

        public TooltipTextBlock ClaimTextBlock()
        {
            if (_textBlockCursor < _textBlocks.Count)
            {
                return _textBlocks[_textBlockCursor++];
            }

            TooltipTextBlock block = _textBlocks.Count == 0
                ? textTemplate
                : Instantiate(textTemplate, _transformToMove);
            _textBlocks.Add(block);
            _textBlockCursor++;
            return block;
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
