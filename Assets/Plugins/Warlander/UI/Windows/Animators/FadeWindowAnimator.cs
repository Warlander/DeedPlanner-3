using System;
using DG.Tweening;
using UnityEngine;

namespace Warlander.UI.Windows.Animators
{
    public class FadeWindowAnimator : WindowAnimator
    {
        [SerializeField] private CanvasGroup _backgroundGroup;
        [SerializeField] private CanvasGroup _windowGroup;

        [SerializeField] private float _animationTime = 0.15f;
        
        public override bool ShowingSupported => true;
        public override bool ClosingSupported => true;

        public override void ApplyStartingState()
        {
            _backgroundGroup.alpha = 0;
            _windowGroup.alpha = 0;
        }
        
        public override void AnimateShowingWindow(Action onDone)
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(_backgroundGroup.DOFade(1, _animationTime));
            sequence.Join(_windowGroup.DOFade(1, _animationTime));
            sequence.AppendCallback(() => onDone?.Invoke());
        }

        public override void AnimateClosingWindow(Action onDone)
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(_backgroundGroup.DOFade(0, _animationTime));
            sequence.Join(_windowGroup.DOFade(0, _animationTime));
            sequence.AppendCallback(() => onDone?.Invoke());
        }

        private void OnDestroy()
        {
            this.DOKill();
        }
    }
}