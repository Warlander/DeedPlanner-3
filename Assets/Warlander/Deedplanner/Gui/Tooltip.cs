using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public class Tooltip : MonoBehaviour
    {

        [SerializeField] private TMP_Text text;

        private RectTransform rectTransform;

        public string Value
        {
            get => text.text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    gameObject.SetActive(false);
                    return;
                }
                
                gameObject.SetActive(true);
                text.text = value;
            }
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        private void Update()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 cursorCorrection = new Vector2(0, -20);
            rectTransform.anchoredPosition = mousePos + cursorCorrection;
        }

    }
}