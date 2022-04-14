using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Button))]
    public class TopDownMenu : MonoBehaviour
    {
        [SerializeField] private Button blockerPrefab = null;

        [SerializeField] private RectTransform contentTransform = null;
        private Button menuButton;
        private Button blockerButton;

        private void Awake()
        {
            menuButton = GetComponent<Button>();
            if (menuButton)
            {
                menuButton.onClick.AddListener(OnMenuButtonPressed);
            }
            else
            {
                Debug.LogWarning("No button in top down menu, destroying");
                Destroy(this);
            }
        }

        public void ShowMenu()
        {
            CreateBlocker();
            contentTransform.gameObject.SetActive(true);
        }

        public void HideMenu()
        {
            contentTransform.gameObject.SetActive(false);
            Destroy(blockerButton.gameObject);
        }

        private void CreateBlocker()
        {
            Canvas[] parentCanvas = GetComponentsInParent<Canvas>();

            blockerButton = Instantiate(blockerPrefab, parentCanvas[parentCanvas.Length - 1].transform, false);
            blockerButton.onClick.AddListener(OnBlockerButtonPressed);
        }

        private void OnMenuButtonPressed()
        {
            ShowMenu();
        }

        private void OnBlockerButtonPressed()
        {
            HideMenu();
        }

        private void OnDestroy()
        {
            if (menuButton)
            {
                menuButton.onClick.RemoveListener(OnMenuButtonPressed);
            }

            if (blockerButton)
            {
                blockerButton.onClick.RemoveListener(OnBlockerButtonPressed);
                Destroy(blockerButton.gameObject);
            }
        }
    }
}
