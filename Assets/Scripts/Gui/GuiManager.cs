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

            LayoutsList.Add("Single view");
            LayoutsList.Add("Two horizontal views");
            LayoutsList.Add("Two vertical views");
            LayoutsList.Add("Large view on top and two small on bottom");
            LayoutsList.Add("Large view on bottom and two small on top");
            LayoutsList.Add("Four small views");
        }

        public Window CreateWindow(string title, RectTransform content, bool closeable)
        {
            Window windowClone = Instantiate(windowPrefab);
            windowClone.Title = title;
            windowClone.Content = content;
            windowClone.CloseButtonVisible = closeable;

            return windowClone;
        }

        public void OnLayoutsButtonPressed()
        {

        }

    }

}