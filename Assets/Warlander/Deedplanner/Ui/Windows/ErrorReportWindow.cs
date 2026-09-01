using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class ErrorReportWindow : MonoBehaviour
    {
        private const string Title = "An error occurred";

        [SerializeField] private Window _window;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _reportText;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Button _openLogButton;
        [SerializeField] private Button _closeButton;

        private string _report;
        private string _playerLogFolder;

        private void Awake()
        {
            _copyButton.onClick.AddListener(OnCopyClicked);
            _openLogButton.onClick.AddListener(OnOpenLogClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public void ShowReport(string report, string playerLogFolder)
        {
            _report = report;
            _playerLogFolder = playerLogFolder;
            _titleText.text = Title;
            _reportText.text = report;
            _openLogButton.gameObject.SetActive(playerLogFolder != null);
        }

        private void OnCopyClicked()
        {
            GUIUtility.systemCopyBuffer = _report;
        }

        private void OnOpenLogClicked()
        {
            Application.OpenURL("file://" + _playerLogFolder);
        }

        private void OnCloseClicked()
        {
            _window.Close();
        }
    }
}
