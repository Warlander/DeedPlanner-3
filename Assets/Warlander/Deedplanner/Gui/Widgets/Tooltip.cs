using System;
using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public class Tooltip : MonoBehaviour
    {

        [SerializeField] private TMP_Text text = null;

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