using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Warlander.UI.Windows
{
    public class WindowCoordinator
    {
        private const int MinWindowSortOrder = 1000;
        private const int SupportedWindows = 1000;

        [Inject] private DiContainer _diContainer;
        [Inject] private IInstantiator _instantiator;

        private Dictionary<int, Window> _windows = new Dictionary<int, Window>();
        private List<Window> _spawnedPrefabInstances = new List<Window>();

        /// <summary>
        /// Won't create window instance if there's at least one window of given type already instanced.
        /// </summary>
        public Window CreateWindowExclusive(string windowPath, WindowLayer? windowLayer = null)
        {
            return CreateWindow(windowPath, windowLayer, exclusive: true);
        }

        /// <summary>
        /// Won't create window instance if there's at least one window of given type already instanced.
        /// </summary>
        public T CreateWindowExclusive<T>(string windowPath, WindowLayer? windowLayer = null) where T : MonoBehaviour
        {
            return CreateWindow<T>(windowPath, windowLayer, exclusive: true);
        }

        public Window CreateWindow(string windowPath, WindowLayer? windowLayer = null)
        {
            return CreateWindow(windowPath, windowLayer, exclusive: false);
        }

        public T CreateWindow<T>(string windowPath, WindowLayer? windowLayer = null) where T : MonoBehaviour
        {
            return CreateWindow<T>(windowPath, windowLayer, exclusive: false);
        }
        
        private Window CreateWindow(string windowPath, WindowLayer? windowLayer = null, bool exclusive = false)
        {
            return CreateWindow<Window>(windowPath, windowLayer, exclusive);
        }
        
        private T CreateWindow<T>(string windowPath, WindowLayer? windowLayer = null, bool exclusive = false) where T : MonoBehaviour
        {
            Window windowPrefab = Resources.Load<Window>(windowPath);
            windowPrefab.gameObject.SetActive(false);
            
            if (exclusive && _spawnedPrefabInstances.Contains(windowPrefab))
            {
                return null;
            }
            
            _spawnedPrefabInstances.Add(windowPrefab);
            Window window = _instantiator.InstantiatePrefabForComponent<Window>(windowPrefab);

            WindowLayer layer = windowLayer.GetValueOrDefault(window.DefaultLayer);
            int layerNumber = (int)layer;
            int windowIndex = CalculateNextWindowIndex();
            int targetDepth = MinWindowSortOrder + layerNumber * SupportedWindows + windowIndex;
            
            _windows[windowIndex] = window;

            // Make sub-container so dialogs can request to have Window injected.
            DiContainer subContainer = _diContainer.CreateSubContainer();
            subContainer.Bind<Window>().FromInstance(window);
            
            subContainer.InjectGameObject(window.gameObject);

            window.Initialize(targetDepth);
            window.gameObject.SetActive(true);
            
            window.Closed += () =>
            {
                RemoveWindow(windowIndex);
                _spawnedPrefabInstances.Remove(windowPrefab);
            };
            
            window.Show();
            
            return window.GetComponent<T>();
        }

        private void RemoveWindow(int index)
        {
            _windows.Remove(index);
        }

        private int CalculateNextWindowIndex()
        {
            if (_windows.Count == 0)
            {
                return 0;
            }
            
            return _windows.Keys.Max() + 1;
        }
    }
}