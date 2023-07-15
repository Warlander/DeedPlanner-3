using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warlander.UI.Utils
{
    public class ScaleButtonOnClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Selectable _selectable;
        [SerializeField] private Transform _transform;
        [SerializeField] private Vector3 _targetScale = Vector3.one * 0.9f;
        [SerializeField] private float _animationTime = 0.15f;
        [SerializeField] private Ease _ease = Ease.InBack;

        private Vector3 _originalScale;
        
        public void Awake()
        {
            if (_transform == null)
            {
                // Caching costly hidden GetComponent<Transform>() call here.
                _transform = transform;
            }

            _originalScale = _transform.localScale;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (_selectable.interactable)
            {
                _transform.DOComplete();
                _transform.DOScale(_targetScale, _animationTime).SetEase(_ease);
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            _transform.DOComplete();
            _transform.DOScale(_originalScale, _animationTime).SetEase(_ease);
        }

        private void OnDestroy()
        {
            _transform.DOKill();
        }
    }
}