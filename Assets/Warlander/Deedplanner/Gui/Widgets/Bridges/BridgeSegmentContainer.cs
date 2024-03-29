﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Updaters;
using Warlander.UI.Utils;
using Zenject;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeSegmentContainer : MonoBehaviour
    {
        [Inject] private IInstantiator _instantiator;
        [Inject] private BridgesUpdater _bridgesUpdater;
        
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

        public bool ShouldShow => _displayedBridge != null;

        private Bridge DisplayedBridge
        {
            get => _displayedBridge;
            set
            {
                _displayedBridge = value;
            }
        }

        private Bridge _displayedBridge;

        private List<BridgeSegmentItem> _bridgeSegments = new List<BridgeSegmentItem>();

        private void Awake()
        {
            _bridgeSegmentPrefab.gameObject.SetActive(false);
            
            SetBridge(_bridgesUpdater.SelectedBridge);
            
            _bridgesUpdater.SelectedBridgeChanged += BridgesUpdaterOnSelectedBridgeChanged;
        }

        private void BridgesUpdaterOnSelectedBridgeChanged()
        {
            SetBridge(_bridgesUpdater.SelectedBridge);
        }

        private void SetBridge(Bridge bridge)
        {
            DisplayedBridge = bridge;
            RefreshUI();
        }

        private void RefreshUI()
        {
            _pivotAnimator.SetShown(DisplayedBridge != null);
            
            if (DisplayedBridge == null)
            {
                // Display old bridge while no bridge is selected.
                return;
            }
            
            CleanUpBridge();
            SetupNewBridge();
        }

        private void CleanUpBridge()
        {
            foreach (BridgeSegmentItem bridgeSegmentItem in _bridgeSegments)
            {
                Destroy(bridgeSegmentItem.gameObject);
            }
            
            _bridgeSegments.Clear();
        }

        private void SetupNewBridge()
        {
            if (_displayedBridge.IsLongitudinal())
            {
                _bridgeStartImage.sprite = _southSprite;
                _bridgeEndImage.sprite = _northSprite;
            }
            else
            {
                _bridgeStartImage.sprite = _westSprite;
                _bridgeEndImage.sprite = _eastSprite;
            }

            var parts = _displayedBridge.GetBridgeParts();
            for (int i = 0; i < parts.Count; i++)
            {
                BridgeSegmentItem item =
                    _instantiator.InstantiatePrefabForComponent<BridgeSegmentItem>(_bridgeSegmentPrefab, _bridgeSegmentRoot);
                _bridgeSegments.Add(item);
                item.gameObject.SetActive(true);
            }
            
            _bridgeEndImage.transform.SetAsLastSibling();

            UpdateBridge();
        }

        private void UpdateBridge()
        {
            for (int i = 0; i < _bridgeSegments.Count; i++)
            {
                _bridgeSegments[i].Set(_displayedBridge.GetBridgePart(i));
            }
        }

        private void OnDestroy()
        {
            _bridgesUpdater.SelectedBridgeChanged -= BridgesUpdaterOnSelectedBridgeChanged;
        }
    }
}