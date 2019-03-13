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

        public UnityTree GroundsTree {
            get {
                return groundsTree;
            }
        }
        public UnityTree CavesTree {
            get {
                return cavesTree;
            }
        }
        public UnityTree FloorsTree {
            get {
                return floorsTree;
            }
        }
        public UnityTree WallsTree {
            get {
                return wallsTree;
            }
        }
        public UnityList RoofsList {
            get {
                return roofsList;
            }
        }
        public UnityTree ObjectsTree {
            get {
                return objectsTree;
            }
        }

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