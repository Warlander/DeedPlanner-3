using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(TextMeshProUGUI), typeof(ContentSizeFitter), typeof(LayoutElement))]
    public class TextWindow : MonoBehaviour
    {
        private RectTransform rectTransform;
        private TextMeshProUGUI text;
        private LayoutElement layoutElement;

        public TextMeshProUGUI Text => text;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            text = GetComponent<TextMeshProUGUI>();
            layoutElement = GetComponent<LayoutElement>();
        }

        private void Update()
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            layoutElement.minWidth = sizeDelta.x;
            layoutElement.minHeight = sizeDelta.y;
        }
    }
}