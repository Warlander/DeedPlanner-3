using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{

    public class Window : MonoBehaviour
    {

        [SerializeField]
        private GameObject closeButton;

        [SerializeField]
        private RectTransform contentAnchor;

        [SerializeField]
        private TextMeshProUGUI titleGui;

        public bool CloseButtonVisible {
            get {
                return closeButton.activeInHierarchy;
            }
            set {
                closeButton.SetActive(value);
            }
        }

        /// <summary>
        /// Content MUST have minimum width/height to resize from
        /// </summary>
        public RectTransform Content {
            get {
                return contentAnchor.childCount == 0 ? null : (RectTransform) contentAnchor.GetChild(0);
            }
            set {
                value.transform.SetParent(contentAnchor);
            }
        }

        public string Title {
            get {
                return titleGui.text;
            }
            set {
                titleGui.text = value;
            }
        }

    }

}