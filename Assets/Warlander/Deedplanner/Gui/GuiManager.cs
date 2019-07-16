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
        [SerializeField]
        private Window aboutWindow = null;

        public UnityTree GroundsTree => groundsTree;
        public UnityTree CavesTree => cavesTree;
        public UnityTree FloorsTree => floorsTree;
        public UnityTree WallsTree => wallsTree;
        public UnityList RoofsList => roofsList;
        public UnityTree ObjectsTree => objectsTree;

        public Window ResizeMapWindow => resizeMapWindow;
        public Window ClearMapWindow => clearMapWindow;
        public Window SaveMapWindow => saveMapWindow;
        public Window LoadMapWindow => loadMapWindow;
        public Window GraphicsSettingsWindow => graphicsSettingsWindow;
        public Window InputSettingsWindow => inputSettingsWindow;
        public Window AboutWindow => aboutWindow;

        public GuiManager()
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