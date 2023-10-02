using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.UI.Windows
{
    /// <summary>
    /// Overall idea of windows:
    /// - Windows are always created via WindowCoordinator which makes sure windows play nice with each other.
    /// - Most windows typically have at least 3 MonoBehaviours:
    ///     - Window itself, handles core functionality of the window.
    ///     - Implementation(s) of WindowAnimator for window showing and closing.
    ///       They can be separate or the same behaviour depending on implementation and expected animations.
    ///     - Per-window logic handling. (SettingsWindow, AboutWindow and so on)
    /// </summary>
    public class Window : MonoBehaviour
    {
        [Header("Window components")]
        [SerializeField] private Canvas _windowCanvas;
        [SerializeField] private Button _outsideButton;
        [SerializeField] private Button _closeButton;

        [Header("Window controls")]
        [SerializeField] private bool _clickOutsideToClose;
        [SerializeField] private WindowLayer _defaultLayer;
        
        [Header("Animations")]
        [SerializeField] private WindowAnimator _windowShowingAnimator;
        [SerializeField] private WindowAnimator _windowClosingAnimator;
        
        public event Action Shown;
        public event Action Closing;
        public event Action Closed;

        public WindowLayer DefaultLayer => _defaultLayer;
        
        public WindowState WindowState
        {
            get => _windowState;
            private set
            {
                _windowState = value;

                switch (_windowState)
                {
                    case WindowState.Shown:
                        Shown?.Invoke();
                        break;
                    case WindowState.Closing:
                        Closing?.Invoke();
                        break;
                    case WindowState.Closed:
                        Closed?.Invoke();
                        Destroy(gameObject);
                        break;
                }
            }
        }

        private WindowState _windowState = WindowState.NotInitialized;

        private void Awake()
        {
            if (_outsideButton)
            {
                _outsideButton.onClick.AddListener(OnOutsideClick);
            }
            if (_closeButton)
            {
                _closeButton.onClick.AddListener(OnCloseClick);
            }
        }
        
        internal void Initialize(int sortOrder)
        {
            _windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _windowCanvas.overrideSorting = true;
            _windowCanvas.sortingOrder = sortOrder;
        }

        public void Show()
        {
            WindowState = WindowState.Showing;
            
            if (_windowShowingAnimator != null && _windowShowingAnimator.ShowingSupported)
            {
                _windowShowingAnimator.ApplyStartingState();
                _windowShowingAnimator.AnimateShowingWindow(() =>
                {
                    WindowState = WindowState.Shown;
                });
            }
            else
            {
                WindowState = WindowState.Shown;
            }
        }
        
        public void Close()
        {
            WindowState = WindowState.Closing;
            
            if (_windowClosingAnimator != null && _windowClosingAnimator.ClosingSupported)
            {
                _windowClosingAnimator.AnimateClosingWindow(() =>
                {
                    WindowState = WindowState.Closed;
                });
            }
            else
            {
                WindowState = WindowState.Closed;
            }
        }

        private void OnOutsideClick()
        {
            if (_clickOutsideToClose)
            {
                Close();
            }
        }

        private void OnValidate()
        {
            if (_windowShowingAnimator != null && _windowShowingAnimator.ShowingSupported == false)
            {
                Debug.LogError($"Showing animation not supported by animator on {gameObject.name}.");
            }
            
            if (_windowClosingAnimator != null && _windowClosingAnimator.ShowingSupported == false)
            {
                Debug.LogError($"Showing animation not supported by animator on {gameObject.name}.");
            }
        }

        private void OnCloseClick()
        {
            Close();
        }
    }
}