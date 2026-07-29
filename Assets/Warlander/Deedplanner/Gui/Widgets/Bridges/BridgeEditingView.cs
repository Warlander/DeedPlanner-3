using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeEditingView : MonoBehaviour, IBridgeEditingView
    {
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _cancelButton;

        public event Action DeleteClicked;
        public event Action CancelClicked;
        public event Action BecameActive;
        public event Action BecameInactive;

        public bool IsActive => gameObject.activeInHierarchy;

        private void Awake()
        {
            _deleteButton.onClick.AddListener(OnDeleteClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            _deleteButton.onClick.RemoveListener(OnDeleteClicked);
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        private void OnEnable()
        {
            BecameActive?.Invoke();
        }

        private void OnDisable()
        {
            BecameInactive?.Invoke();
        }

        public void SetDeleteButtonVisible(bool visible)
        {
            _deleteButton.gameObject.SetActive(visible);
        }

        public void SetCancelButtonVisible(bool visible)
        {
            _cancelButton.gameObject.SetActive(visible);
        }

        private void OnDeleteClicked()
        {
            DeleteClicked?.Invoke();
        }

        private void OnCancelClicked()
        {
            CancelClicked?.Invoke();
        }
    }
}
