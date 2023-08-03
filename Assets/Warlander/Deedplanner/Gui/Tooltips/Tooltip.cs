using TMPro;
using UnityEngine;
using DG.Tweening;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform _referenceCanvasTransform;
        [SerializeField] private RectTransform _transformToMove;
        [SerializeField] private float animationSpeed = 10f;

        public string Value
        {
            get => text.text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    canvasGroup.DOKill();
                    canvasGroup.DOFade(0, animationSpeed).SetEase(Ease.Linear).SetSpeedBased();
                    return;
                }
                
                canvasGroup.DOKill();
                canvasGroup.DOFade(1, animationSpeed).SetEase(Ease.Linear).SetSpeedBased();
                gameObject.SetActive(true);
                text.text = value;
            }
        }

        private void Awake()
        {
            canvasGroup.alpha = 0;
        }

        private void OnEnable()
        {
            UpdatePosition();
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 cursorCorrection = new Vector2(0, -20);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_referenceCanvasTransform, Input.mousePosition, null,
                out Vector2 localPos);
            _transformToMove.localPosition = localPos + cursorCorrection;
        }
    }
}