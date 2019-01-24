using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{

    public class GuiManager : MonoBehaviour
    {

        public static GuiManager Instance { get; private set; }

        [SerializeField]
        private Window windowPrefab;

        [SerializeField]
        private UnityList LayoutsList;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public Window CreateWindow(string title, RectTransform content, bool closeable)
        {
            Window windowClone = Instantiate(windowPrefab);
            windowClone.Title = title;
            windowClone.Content = content;
            windowClone.CloseButtonVisible = closeable;

            return windowClone;
        }

    }

}