using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui
{
    public class GuiManager : MonoBehaviour
    {
        public static GuiManager Instance { get; private set; }

        [SerializeField] private RectTransform[] interfaceTransforms = null;

        [SerializeField] private RectTransform windowsRoot = null;
        [SerializeField] private Window windowPrefab = null;
        [SerializeField] private TextWindow textWindowPrefab = null;
        
        [SerializeField] private UnityTree groundsTree = null;
        [SerializeField] private UnityTree cavesTree = null;
        [SerializeField] private UnityTree floorsTree = null;
        [SerializeField] private UnityTree wallsTree = null;
        [SerializeField] private UnityList roofsList = null;
        [SerializeField] private UnityTree objectsTree = null;

        [SerializeField] private WindowMapping[] windows = null;
        
        public UnityTree GroundsTree => groundsTree;
        public UnityTree CavesTree => cavesTree;
        public UnityTree FloorsTree => floorsTree;
        public UnityTree WallsTree => wallsTree;
        public UnityList RoofsList => roofsList;
        public UnityTree ObjectsTree => objectsTree;

        public GuiManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                foreach (RectTransform interfaceTransform in interfaceTransforms)
                {
                    interfaceTransform.gameObject.SetActive(!interfaceTransform.gameObject.activeSelf);
                }
            }
        }

        public Window ShowWindow(WindowId id)
        {
            foreach (WindowMapping windowMapping in windows)
            {
                if (windowMapping.Id == id)
                {
                    windowMapping.Window.ShowWindow();
                    return windowMapping.Window;
                }
            }

            throw new ArgumentException("Unable to find window for ID " + id);
        }

        /// <summary>
        /// Utility function to be called from inspector-bound event callbacks
        /// </summary>
        public void ShowLayoutsWindow()
        {
            ShowWindow(WindowId.Layouts);
        }

        public Window CreateWindow(string title, RectTransform content, bool closeable)
        {
            Window windowClone = Instantiate(windowPrefab, windowsRoot);
            windowClone.Title = title;
            windowClone.Content = content;
            windowClone.CloseButtonVisible = closeable;
            windowClone.gameObject.SetActive(false);
            windowClone.DestroyOnClose = true;

            return windowClone;
        }

        public Window CreateTextWindow(string title, string text)
        {
            TextWindow textWindow = Instantiate(textWindowPrefab);
            textWindow.Text.text = text;

            return CreateWindow(title, textWindow.GetComponent<RectTransform>(), true);
        }

        [Serializable]
        private struct WindowMapping
        {
            [SerializeField] private WindowId id;
            [SerializeField] private Window window;

            public WindowId Id => id;
            public Window Window => window;

            private WindowMapping(WindowId newId, Window newWindow)
            {
                id = newId;
                window = newWindow;
            }
        }
    }
}