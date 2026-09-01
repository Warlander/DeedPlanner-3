using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class DeleteSaveWindowView : MonoBehaviour
    {
        private Window _window;
        private Action _onConfirm;

        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

        private void Start()
        {
            _confirmButton.onClick.AddListener(ConfirmOnClick);
            _cancelButton.onClick.AddListener(() => _window.Close());
        }

        public void SetMessage(string message, Action onConfirm)
        {
            _messageText.text = message;
            _onConfirm = onConfirm;
        }

        private void ConfirmOnClick()
        {
            _onConfirm?.Invoke();
            _window.Close();
        }
    }
}
