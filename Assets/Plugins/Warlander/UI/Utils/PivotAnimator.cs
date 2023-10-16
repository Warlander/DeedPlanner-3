using DG.Tweening;
using UnityEngine;

namespace Warlander.UI.Utils
{
    public class PivotAnimator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform _transform;
        
        [Header("Animation Controls")]
        [SerializeField] private Ease _ease = Ease.InOutBack;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Vector2 _showPivot = Vector2.one * 0.5f;
        [SerializeField] private Vector2 _hidePivot = Vector2.one * 0.5f;
        [SerializeField] private bool _disableWhenHidden = true;
        
        [Header("Defaults")]
        [SerializeField] private bool _shownByDefault = true;

        private bool _shown;

        private void Awake()
        {
            _shown = _shownByDefault;
            AnimatePivotChange(_shown, true);
        }

        public void SetShown(bool shown)
        {
            _shown = shown;
            AnimatePivotChange(_shown, false);
        }

        private void AnimatePivotChange(bool shown, bool instant)
        {
            if (shown)
            {
                gameObject.SetActive(true);
            }

            this.DOComplete(true);
            
            _transform.DOPivot(GetPivotForShowState(shown), _duration)
                .SetEase(_ease)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    if (shown == false && _disableWhenHidden)
                    {
                        gameObject.SetActive(false);
                    }
                });

            if (instant)
            {
                this.DOComplete(true);
            }
        }

        private Vector2 GetPivotForShowState(bool shown)
        {
            if (shown)
            {
                return _showPivot;
            }
            else
            {
                return _hidePivot;
            }
        }

        private void OnDestroy()
        {
            this.DOKill();
        }
    }
}