using TMPro;
using UnityEngine;
using DG.Tweening;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] private TMP_Text text = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private float animationSpeed = 10f;

        private RectTransform rectTransform;

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
            rectTransform = GetComponent<RectTransform>();
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
            Vector2 mousePos = Input.mousePosition;
            Vector2 cursorCorrection = new Vector2(0, -20);
            rectTransform.anchoredPosition = mousePos + cursorCorrection;
        }

    }
}