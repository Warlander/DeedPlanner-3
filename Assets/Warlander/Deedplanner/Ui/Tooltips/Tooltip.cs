using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Warlander.Deedplanner.Inputs;
using Warlander.UI.Windows;
using VContainer;

namespace Warlander.Deedplanner.Ui.Tooltips
{
    public class Tooltip : MonoBehaviour
    {
        [Inject] private DPInput _input;

        [SerializeField] private TooltipTextBlock textTemplate;
        [SerializeField] private CanvasGroup canvasGroup;
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

            LayoutRebuilder.ForceRebuildLayoutImmediate(_transformToMove);
            UpdatePosition();
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

        private void UpdatePosition()
        {
            Vector2 focusPos = _input.MapInputShared.FocusPosition.ReadValue<Vector2>();

            Rect referenceCanvasRect = _referenceCanvasTransform.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _referenceCanvasTransform, focusPos, null, out Vector2 localPos);

            bool isPointerOnScreen = referenceCanvasRect.Contains(localPos);

            // Don't update pivot and connection if pointer goes off-screen - this will cause sudden tooltip shift otherwise.
            if (isPointerOnScreen)
            {
                Vector2 pivot = SelectPivot(referenceCanvasRect, localPos, _transformToMove.rect.size,
                    _cursorCorrection);
                _transformToMove.pivot = pivot;
                _cursorCorrectionToUse = new Vector2(
                    pivot.x == 0 ? _cursorCorrection.x : -_cursorCorrection.x,
                    pivot.y == 1 ? _cursorCorrection.y : -_cursorCorrection.y);
            }

            _transformToMove.localPosition = localPos + _cursorCorrectionToUse;
        }

        private static Vector2 SelectPivot(Rect bounds, Vector2 focusPosition, Vector2 tooltipSize,
            Vector2 cursorCorrection)
        {
            bool fitsRight = focusPosition.x + cursorCorrection.x + tooltipSize.x <= bounds.xMax;
            bool fitsBelow = focusPosition.y + cursorCorrection.y - tooltipSize.y >= bounds.yMin;
            return new Vector2(fitsRight ? 0 : 1, fitsBelow ? 1 : 0);
        }

        private void OnDestroy()
        {
            canvasGroup.DOKill();
        }
    }
}
