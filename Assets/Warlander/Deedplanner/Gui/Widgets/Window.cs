using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public class Window : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private GameObject closeButton = null;
        [SerializeField] private RectTransform contentAnchor = null;
        [SerializeField] private TextMeshProUGUI titleGui = null;

        public bool CloseButtonVisible {
            get => closeButton.activeInHierarchy;
            set => closeButton.SetActive(value);
        }

        /// <summary>
        /// Content MUST have minimum width/height to resize from
        /// </summary>
        public RectTransform Content {
            get => contentAnchor.childCount == 0 ? null : (RectTransform) contentAnchor.GetChild(0);
            set => value.transform.SetParent(contentAnchor);
        }

        public string Title {
            get => titleGui.text;
            set => titleGui.text = value;
        }

        private bool windowShown = false;

        private void Awake()
        {
            windowShown = gameObject.activeSelf;
        }

        private void OnEnable()
        {
            if (!windowShown)
            {
                Debug.LogWarning("\nwindowShown\" flag was disabled when enabling the window - please make sure window is shown using \"ShowWindow()\" instead of enabling directly.");
            }
        }

        public void ShowWindow()
        {
            if (windowShown)
            {
                return;
            }

            windowShown = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 0.15f).SetEase(Ease.Linear);
            transform.SetAsLastSibling();
        }

        public void HideWindow()
        {
            if (!windowShown)
            {
                return;
            }

            windowShown = false;
            canvasGroup.DOFade(0, 0.15f).SetEase(Ease.Linear).OnComplete(() => gameObject.SetActive(false));
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        private void OnDisable()
        {
            if (windowShown)
            {
                Debug.LogWarning("\nwindowShown\" flag was active when disabling the window - please make sure window is hidden using \"HideWindow()\" instead of disabling directly.");
            }
            windowShown = false;
        }
    }
}