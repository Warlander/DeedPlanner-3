using TMPro;
using UnityEngine;
using DG.Tweening;
using Warlander.Deedplanner.Inputs;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class Tooltip : MonoBehaviour
    {
        [Inject] private DPInput _input;
        
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
            Vector2 focusPos = _input.MapInputShared.FocusPosition.ReadValue<Vector2>();
            Vector2 cursorCorrection = new Vector2(0, -20);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_referenceCanvasTransform, focusPos, null,
                out Vector2 localPos);
            _transformToMove.localPosition = localPos + cursorCorrection;
        }
    }
}