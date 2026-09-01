using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System;
using System.Linq;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Bridges.Widgets
{
    public class BridgeSegmentBarPresenter : IInitializable, IDisposable
    {
        private const string IncorrectTooltip =
            "Too far from a support - add one nearby or restore the removed support";
        private const string AdjacentSupportsTooltip =
            "Supports cannot be next to each other";

        private readonly IBridgeSegmentBarView _view;
        private readonly BridgesUpdater _bridgesUpdater;
        private readonly MapHandler _mapHandler;
        private readonly IDataCatalog _dataCatalog;

        private Bridge _bridge;
        private bool[] _pendingSupports;
        private bool _editable;
        private string _tooltipSuffix;
        private bool _pavingMode;
        private BridgePavementData[] _pavingChoices;
        private int _selectedPavingIndex;

        public BridgeSegmentBarPresenter(IBridgeSegmentBarView view, BridgesUpdater bridgesUpdater,
            MapHandler mapHandler, IDataCatalog dataCatalog)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.SegmentClicked += OnSegmentClicked;
            _view.SegmentHovered += OnSegmentHovered;
            _view.PavingModeChanged += OnPavingModeChanged;
            _view.PavingSelected += OnPavingSelected;
            _bridgesUpdater.SelectedBridgeChanged += OnSelectedBridgeChanged;

            // index 0 is null, the eraser ("no paving")
            _pavingChoices = new BridgePavementData[] { null }.Concat(_dataCatalog.GetAllBridgePavements()).ToArray();
            _view.SetPavingMode(false);

            OnSelectedBridgeChanged();
        }

        public void Dispose()
        {
            _view.SegmentClicked -= OnSegmentClicked;
            _view.SegmentHovered -= OnSegmentHovered;
            _view.PavingModeChanged -= OnPavingModeChanged;
            _view.PavingSelected -= OnPavingSelected;
            _bridgesUpdater.SelectedBridgeChanged -= OnSelectedBridgeChanged;
        }

        private void OnSegmentHovered(int index)
        {
            if (_pavingMode)
            {
                return;
            }

            _bridgesUpdater.SetHoveredSegment(index);
        }

        private void OnSelectedBridgeChanged()
        {
            _bridge = _bridgesUpdater.SelectedBridge;

            if (_bridge == null)
            {
                _pendingSupports = null;
            }
            else
            {
                _pendingSupports = _bridge.GetSupportPositions();
                _editable = _bridge.Type == BridgeType.Flat;
                _tooltipSuffix = GetTooltipSuffix(_bridge.Type);
                _view.SetSupportsModeAvailable(_bridge.Data.CanBePaved);
            }

            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_pavingMode)
            {
                _view.ShowPavingPalette(_pavingChoices, _selectedPavingIndex);
            }
            else if (_bridge == null)
            {
                _view.ShowBridge(null, false, null);
            }
            else
            {
                _view.ShowBridge(_bridge, _editable, _tooltipSuffix);
            }
        }

        private void OnPavingModeChanged(bool pavingMode)
        {
            _pavingMode = pavingMode;
            _view.SetPavingMode(pavingMode);
            PushBrush();
            RefreshDisplay();
        }

        private void OnPavingSelected(int choiceIndex)
        {
            _selectedPavingIndex = choiceIndex;
            _view.SetPavingSelection(choiceIndex);
            PushBrush();
        }

        private void PushBrush()
        {
            _bridgesUpdater.SetPavingBrush(_pavingMode, _pavingChoices[_selectedPavingIndex]);
        }

        private void OnSegmentClicked(int index)
        {
            if (_pavingMode || !_editable || _bridge == null || _pendingSupports == null
                || index < 0 || index >= _pendingSupports.Length)
            {
                return;
            }

            _pendingSupports[index] = !_pendingSupports[index];

            bool valid = BridgeStructureUtils.ConstructFlatBridge(_pendingSupports, out BridgePartType?[] preview);
            if (_bridge.Data.Name == "wood")
            {
                preview = BridgeStructureUtils.SubstituteWoodParts(preview);
            }

            if (!valid)
            {
                string incorrectTooltip = IncorrectTooltip;
                if (HasAdjacentSupports(_pendingSupports))
                {
                    incorrectTooltip = AdjacentSupportsTooltip;
                    MarkAdjacentSupportsIncorrect(preview);
                }

                _view.ShowPreview(_bridge, preview, incorrectTooltip);
                _view.SetInvalidState(true);
                return;
            }

            string oldSegments = _bridge.GetSegmentsString();
            string newSegments = BridgePartTypeUtils.EncodeSegments(preview.Select(segment => segment.Value).ToArray());

            if (newSegments == oldSegments)
            {
                _view.ShowBridge(_bridge, true, _tooltipSuffix);
                return;
            }

            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(
                new BridgeSegmentsChangeCommand(map, _bridge, oldSegments, newSegments));
            map.CommandManager.FinishAction();
        }

        private bool HasAdjacentSupports(bool[] supports)
        {
            for (int i = 1; i < supports.Length; i++)
            {
                if (supports[i] && supports[i - 1])
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkAdjacentSupportsIncorrect(BridgePartType?[] preview)
        {
            for (int i = 1; i < _pendingSupports.Length; i++)
            {
                if (_pendingSupports[i] && _pendingSupports[i - 1])
                {
                    preview[i] = null;
                }
            }
        }

        private string GetTooltipSuffix(BridgeType type)
        {
            switch (type)
            {
                case BridgeType.Rope:
                    return " - rope bridges have a fixed layout, no supports to edit";
                case BridgeType.Arched:
                    return " - arch shape is determined by length, no supports to edit";
                default:
                    return null;
            }
        }
    }
}
