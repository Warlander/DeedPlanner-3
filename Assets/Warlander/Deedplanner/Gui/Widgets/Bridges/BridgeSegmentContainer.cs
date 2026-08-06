using System;
using System.Collections.Generic;
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

        [Inject] private IObjectResolver _resolver;

        [SerializeField] private BridgeSegmentItem _bridgeSegmentPrefab;
        [SerializeField] private PivotAnimator _pivotAnimator;
        [SerializeField] private Transform _bridgeSegmentRoot;
        [SerializeField] private Image _bridgeStartImage;
        [SerializeField] private Image _bridgeEndImage;

        [SerializeField] private Sprite _incorrectSectionSprite;
        [SerializeField] private Sprite _northSprite;
        [SerializeField] private Sprite _southSprite;
        [SerializeField] private Sprite _westSprite;
        [SerializeField] private Sprite _eastSprite;

        public event Action<int> SegmentClicked;

        private readonly List<BridgeSegmentItem> _bridgeSegments = new List<BridgeSegmentItem>();

        private void Awake()
        {
            _bridgeSegmentPrefab.gameObject.SetActive(false);
        }

        public void ShowBridge(Bridge bridge, bool editable, string tooltipSuffix)
        {
            _pivotAnimator.SetShown(bridge != null);

            if (bridge == null)
            {
                return;
            }

            SetInvalidState(false);
            SetupOrientationArrows(bridge);
            CleanUpSegments();

            for (int i = 0; i < bridge.SegmentCount; i++)
            {
                BridgeSegmentItem item = CreateItem(i);
                item.Set(bridge.GetSegmentPart(i), tooltipSuffix);
                item.SetClickable(editable);
            }

            _bridgeEndImage.transform.SetAsLastSibling();
        }

        public void ShowPreview(Bridge bridge, BridgePartType?[] previewSegments, string incorrectTooltip)
        {
            if (bridge == null)
            {
                return;
            }

            SetupOrientationArrows(bridge);
            CleanUpSegments();

            for (int i = 0; i < previewSegments.Length; i++)
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
