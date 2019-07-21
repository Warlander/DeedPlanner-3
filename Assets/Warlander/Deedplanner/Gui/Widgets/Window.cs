using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Widgets
{

    public class Window : MonoBehaviour
    {

        [SerializeField]
        private GameObject closeButton = null;
        [SerializeField]
        private RectTransform contentAnchor = null;
        [SerializeField]
        private TextMeshProUGUI titleGui = null;

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

    }

}