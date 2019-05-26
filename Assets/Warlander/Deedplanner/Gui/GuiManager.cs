using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{

    public class GuiManager : MonoBehaviour
    {

        public static GuiManager Instance { get; private set; }

        [SerializeField]
        private Window windowPrefab = null;

        [SerializeField]
        private UnityTree groundsTree = null;
        [SerializeField]
        private UnityTree cavesTree = null;
        [SerializeField]
        private UnityTree floorsTree = null;
        [SerializeField]
        private UnityTree wallsTree = null;
        [SerializeField]
        private UnityList roofsList = null;
        [SerializeField]
        private UnityTree objectsTree = null;

        [SerializeField]
        private Window resizeMapWindow = null;
        [SerializeField]
        private Window clearMapWindow = null;
        [SerializeField]
        private Window saveMapWindow = null;
        [SerializeField]
        private Window loadMapWindow = null;
        [SerializeField]
        private Window graphicsSettingsWindow = null;
        [SerializeField]
        private Window inputSettingsWindow = null;

        public UnityTree GroundsTree { get => groundsTree; }
        public UnityTree CavesTree { get => cavesTree; }
        public UnityTree FloorsTree { get => floorsTree; }
        public UnityTree WallsTree { get => wallsTree; }
        public UnityList RoofsList { get => roofsList; }
        public UnityTree ObjectsTree { get => objectsTree; }

        public Window ResizeMapWindow { get => resizeMapWindow; }
        public Window ClearMapWindow { get => clearMapWindow; }
        public Window SaveMapWindow { get => saveMapWindow; }
        public Window LoadMapWindow { get => loadMapWindow; }
        public Window GraphicsSettingsWindow { get => graphicsSettingsWindow; }
        public Window InputSettingsWindow { get => inputSettingsWindow; }
        

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