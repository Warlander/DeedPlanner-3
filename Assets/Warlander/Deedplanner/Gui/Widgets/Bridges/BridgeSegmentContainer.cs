using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.UI.Utils;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeSegmentContainer : MonoBehaviour, IBridgeSegmentBarView
    {
        private static readonly Color ValidArrowColor = Color.white;
        private static readonly Color InvalidArrowColor = Color.red;

        private const float SegmentTilePitch = 33f; // 32px tile + 1px layout spacing
        private const float SegmentArrowsWidth = 64f;

        [Inject] private IObjectResolver _resolver;

        [SerializeField] private BridgeSegmentItem _bridgeSegmentPrefab;
        [SerializeField] private PivotAnimator _pivotAnimator;
        [SerializeField] private Transform _bridgeSegmentRoot;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private Image _bridgeStartImage;
        [SerializeField] private Image _bridgeEndImage;
        [SerializeField] private Toggle _supportsModeToggle;
        [SerializeField] private Toggle _pavingModeToggle;
        [SerializeField] private TMP_Dropdown _pavingDropdown;
        [SerializeField] private Button _applyToAllButton;

        [SerializeField] private Sprite _incorrectSectionSprite;
        [SerializeField] private Sprite _northSprite;
        [SerializeField] private Sprite _southSprite;
        [SerializeField] private Sprite _westSprite;
        [SerializeField] private Sprite _eastSprite;

        public event Action<int> SegmentClicked;
        public event Action<int> SegmentHovered;
        public event Action<bool> PavingModeChanged;
        public event Action<int> PavingSelected;
        public event Action ApplyToAllClicked;

        private readonly List<BridgeSegmentItem> _bridgeSegments = new List<BridgeSegmentItem>();
        private readonly Dictionary<BridgePavementData, Sprite> _pavingSprites =
            new Dictionary<BridgePavementData, Sprite>();
        private int _hoveredSegment = -1;

        private Bridge _lastBridge;
        private bool _lastEditable;
        private string _lastTooltipSuffix;
        private BridgePartType?[] _lastPreviewSegments;
        private string _lastIncorrectTooltip;
        private bool _lastShowWasPreview;
        private float _lastViewportWidth;

        private void Awake()
        {
            _bridgeSegmentPrefab.gameObject.SetActive(false);

            _supportsModeToggle.onValueChanged.AddListener(OnModeToggleChanged);
            _pavingModeToggle.onValueChanged.AddListener(OnModeToggleChanged);
            _pavingDropdown.onValueChanged.AddListener(index => PavingSelected?.Invoke(index));
            _applyToAllButton.onClick.AddListener(() => ApplyToAllClicked?.Invoke());
        }

        // Resolved centrally each frame: per-item enter/exit events can arrive in any
        // order within one frame on fast moves, letting a stale exit clear a fresh hover.
        private void Update()
        {
            int hovered = -1;
            for (int i = 0; i < _bridgeSegments.Count; i++)
            {
                if (_bridgeSegments[i].IsPointerOver)
                {
                    hovered = i;
                    break;
                }
            }

            if (hovered != _hoveredSegment)
            {
                _hoveredSegment = hovered;
                SegmentHovered?.Invoke(hovered);
            }

            // Segment count adapts to the space actually left by the controls, so a
            // viewport resize (window resize) rebuilds the strip to use it all.
            if (_lastBridge != null && !Mathf.Approximately(_viewport.rect.width, _lastViewportWidth))
            {
                _lastViewportWidth = _viewport.rect.width;
                ReshowLast();
            }
        }

        private void OnModeToggleChanged(bool isOn)
        {
            if (isOn)
            {
                PavingModeChanged?.Invoke(_pavingModeToggle.isOn);
            }
        }

        private void ReshowLast()
        {
            if (_lastShowWasPreview)
            {
                ShowPreview(_lastBridge, _lastPreviewSegments, _lastIncorrectTooltip);
            }
            else
            {
                ShowBridge(_lastBridge, _lastEditable, _lastTooltipSuffix);
            }
        }

        public void ShowBridge(Bridge bridge, bool editable, string tooltipSuffix)
        {
            _lastBridge = bridge;
            _lastEditable = editable;
            _lastTooltipSuffix = tooltipSuffix;
            _lastShowWasPreview = false;

            _pivotAnimator.SetShown(bridge != null);

            if (bridge == null)
            {
                return;
            }

            SetInvalidState(false);
            SetupOrientationArrows(bridge);
            CleanUpSegments();

            int count = Mathf.Min(bridge.SegmentCount, GetMaxVisibleSegments());
            for (int i = 0; i < count; i++)
            {
                BridgeSegmentItem item = CreateItem(i);
                item.Set(bridge.GetSegmentPart(i), tooltipSuffix);
                item.SetClickable(editable);
            }

            _bridgeEndImage.transform.SetAsLastSibling();
        }

        public void ShowPreview(Bridge bridge, BridgePartType?[] previewSegments, string incorrectTooltip)
        {
            _lastBridge = bridge;
            _lastPreviewSegments = previewSegments;
            _lastIncorrectTooltip = incorrectTooltip;
            _lastShowWasPreview = true;

            if (bridge == null)
            {
                return;
            }

            SetupOrientationArrows(bridge);
            CleanUpSegments();

            int count = Mathf.Min(previewSegments.Length, GetMaxVisibleSegments());
            for (int i = 0; i < count; i++)
            {
                BridgeSegmentItem item = CreateItem(i);
                if (previewSegments[i].HasValue)
                {
                    item.SetPreview(bridge.Data.GetUISpriteForPart(previewSegments[i].Value), previewSegments[i].Value);
                }
                else
                {
                    item.SetIncorrect(_incorrectSectionSprite, incorrectTooltip);
                }
                item.SetClickable(true);
            }

            _bridgeEndImage.transform.SetAsLastSibling();
        }

        public void SetInvalidState(bool invalid)
        {
            Color color = invalid ? InvalidArrowColor : ValidArrowColor;
            _bridgeStartImage.color = color;
            _bridgeEndImage.color = color;
        }

        public void SetPavingChoices(BridgePavementData[] choices, int selectedIndex)
        {
            _pavingDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (BridgePavementData choice in choices)
            {
                options.Add(new TMP_Dropdown.OptionData(choice == null ? "no paving" : choice.Name));
            }

            _pavingDropdown.AddOptions(options);
            _pavingDropdown.SetValueWithoutNotify(selectedIndex);
            _pavingDropdown.RefreshShownValue();

            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] != null)
                {
                    LoadPavingIconAsync(i, choices[i]);
                }
            }
        }

        public void SetPavingMode(bool pavingMode)
        {
            _supportsModeToggle.SetIsOnWithoutNotify(!pavingMode);
            _pavingModeToggle.SetIsOnWithoutNotify(pavingMode);
            _pavingDropdown.gameObject.SetActive(pavingMode);
            _applyToAllButton.gameObject.SetActive(pavingMode);
        }

        public void SetModeSwitchAvailable(bool available)
        {
            _supportsModeToggle.gameObject.SetActive(available);
            _pavingModeToggle.gameObject.SetActive(available);
        }

        public void ShowPavements(Bridge bridge, BridgePavementData[] pavements)
        {
            int count = Mathf.Min(pavements.Length, _bridgeSegments.Count);
            for (int i = 0; i < count; i++)
            {
                BridgePavementData pavement = pavements[i];
                if (pavement == null)
                {
                    _bridgeSegments[i].SetPaving(null, "no paving");
                }
                else if (_pavingSprites.TryGetValue(pavement, out Sprite sprite) && sprite)
                {
                    _bridgeSegments[i].SetPaving(sprite, pavement.Name);
                }
                else
                {
                    LoadPavingSpriteAsync(i, pavement);
                }
            }
        }

        private async void LoadPavingSpriteAsync(int index, BridgePavementData pavement)
        {
            Sprite sprite = await pavement.Tex.LoadOrGetSpriteAsync();
            if (!sprite)
            {
                return;
            }

            _pavingSprites[pavement] = sprite;
            if (index < _bridgeSegments.Count)
            {
                _bridgeSegments[index].SetPaving(sprite, pavement.Name);
            }
        }

        private async void LoadPavingIconAsync(int index, BridgePavementData pavement)
        {
            Sprite sprite = await pavement.Tex.LoadOrGetSpriteAsync();
            if (!sprite || index >= _pavingDropdown.options.Count)
            {
                return;
            }

            _pavingDropdown.options[index].image = sprite;
            if (_pavingDropdown.value == index)
            {
                _pavingDropdown.RefreshShownValue();
            }
        }

        private int GetMaxVisibleSegments()
        {
            float width = _viewport.rect.width;
            return Mathf.Max(1, Mathf.FloorToInt((width - SegmentArrowsWidth) / SegmentTilePitch));
        }

        private BridgeSegmentItem CreateItem(int index)
        {
            BridgeSegmentItem item = _resolver.Instantiate<BridgeSegmentItem>(_bridgeSegmentPrefab, _bridgeSegmentRoot);
            item.gameObject.SetActive(true);

            int capturedIndex = index;
            item.Clicked += () => SegmentClicked?.Invoke(capturedIndex);
            _bridgeSegments.Add(item);
            return item;
        }

        private void SetupOrientationArrows(Bridge bridge)
        {
            if (bridge.IsLongitudinal())
            {
                _bridgeStartImage.sprite = _southSprite;
                _bridgeEndImage.sprite = _northSprite;
            }
            else
            {
                _bridgeStartImage.sprite = _westSprite;
                _bridgeEndImage.sprite = _eastSprite;
            }
        }

        private void CleanUpSegments()
        {
            foreach (BridgeSegmentItem bridgeSegmentItem in _bridgeSegments)
            {
                Destroy(bridgeSegmentItem.gameObject);
            }

            _bridgeSegments.Clear();
        }
    }
}
