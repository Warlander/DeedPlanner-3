using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Editing
{
    public sealed class MirrorUpdaterView : MonoBehaviour, IMirrorUpdaterView
    {
        [SerializeField] private Button _pickVerticalButton;
        [SerializeField] private Button _pickHorizontalButton;
        [SerializeField] private Toggle _verticalToggle;
        [SerializeField] private Toggle _horizontalToggle;
        [SerializeField] private TMP_Text _verticalPosition;
        [SerializeField] private TMP_Text _horizontalPosition;
        [SerializeField] private TMP_Text _pickHint;
        [SerializeField] private Button _clearButton;
        [SerializeField] private TMP_Text _shortcutLabels;
        [SerializeField] private Button _configureShortcutsButton;

        public event Action PickVerticalClicked = delegate { };
        public event Action PickHorizontalClicked = delegate { };
        public event Action<bool> VerticalEnabledChanged = delegate { };
        public event Action<bool> HorizontalEnabledChanged = delegate { };
        public event Action ClearClicked = delegate { };
        public event Action ConfigureShortcutsClicked = delegate { };

        private void Awake()
        {
            _pickVerticalButton.onClick.AddListener(() => PickVerticalClicked());
            _pickHorizontalButton.onClick.AddListener(() => PickHorizontalClicked());
            _verticalToggle.onValueChanged.AddListener(value => VerticalEnabledChanged(value));
            _horizontalToggle.onValueChanged.AddListener(value => HorizontalEnabledChanged(value));
            _clearButton.onClick.AddListener(() => ClearClicked());
            _configureShortcutsButton.onClick.AddListener(() => ConfigureShortcutsClicked());
        }

        public void SetState(bool hasVerticalAxis, bool verticalEnabled, string verticalPosition,
            bool hasHorizontalAxis, bool horizontalEnabled, string horizontalPosition)
        {
            _verticalToggle.SetIsOnWithoutNotify(verticalEnabled);
            _verticalToggle.interactable = hasVerticalAxis;
            _verticalPosition.text = hasVerticalAxis ? verticalPosition : "Not placed";
            _horizontalToggle.SetIsOnWithoutNotify(horizontalEnabled);
            _horizontalToggle.interactable = hasHorizontalAxis;
            _horizontalPosition.text = hasHorizontalAxis ? horizontalPosition : "Not placed";
            _clearButton.interactable = hasVerticalAxis || hasHorizontalAxis;
        }

        public void SetPickMode(bool pickingVertical, bool pickingHorizontal)
        {
            _pickHint.text = pickingVertical
                ? "Click the map to place the vertical axis"
                : pickingHorizontal
                    ? "Click the map to place the horizontal axis"
                    : "Axes snap to the nearest tile center or grid line";
        }

        public void SetShortcutLabels(string labels)
        {
            _shortcutLabels.text = labels;
        }
    }
}
