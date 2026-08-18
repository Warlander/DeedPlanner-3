using System;
using System.Linq;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Updaters;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
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

        private Bridge _bridge;
        private bool[] _pendingSupports;
        private bool _editable;
        private string _tooltipSuffix;
        private bool _pavingMode;
        private BridgePavementData[] _pavingChoices;
        private BridgePavementData _selectedPaving;

        public BridgeSegmentBarPresenter(IBridgeSegmentBarView view, BridgesUpdater bridgesUpdater,
            MapHandler mapHandler)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
        }

        public void Initialize()
        {
            _view.SegmentClicked += OnSegmentClicked;
            _view.SegmentHovered += OnSegmentHovered;
            _view.PavingModeChanged += OnPavingModeChanged;
            _view.PavingSelected += OnPavingSelected;
            _view.ApplyToAllClicked += OnApplyToAllClicked;
            _bridgesUpdater.SelectedBridgeChanged += OnSelectedBridgeChanged;

            // index 0 is null, meaning no paving
            _pavingChoices = new BridgePavementData[] { null }.Concat(Database.BridgePavements.Values).ToArray();
            _view.SetPavingChoices(_pavingChoices, 0);
            _view.SetPavingMode(false);

            OnSelectedBridgeChanged();
        }

        public void Dispose()
        {
            _view.SegmentClicked -= OnSegmentClicked;
            _view.SegmentHovered -= OnSegmentHovered;
            _view.PavingModeChanged -= OnPavingModeChanged;
            _view.PavingSelected -= OnPavingSelected;
            _view.ApplyToAllClicked -= OnApplyToAllClicked;
            _bridgesUpdater.SelectedBridgeChanged -= OnSelectedBridgeChanged;

            if (_bridge != null)
            {
                _bridge.PavementsChanged -= OnPavementsChanged;
            }
        }

        private void OnSegmentHovered(int index)
        {
            _bridgesUpdater.SetHoveredSegment(index);
        }

        private void OnSelectedBridgeChanged()
        {
            if (_bridge != null)
            {
                _bridge.PavementsChanged -= OnPavementsChanged;
            }

            _bridge = _bridgesUpdater.SelectedBridge;

            if (_bridge == null)
            {
                _pendingSupports = null;
                _view.ShowBridge(null, false, null);
                return;
            }

            _bridge.PavementsChanged += OnPavementsChanged;
            _pendingSupports = _bridge.GetSupportPositions();
            _editable = _bridge.Type == BridgeType.Flat;
            _tooltipSuffix = GetTooltipSuffix(_bridge.Type);

            if (!_bridge.Data.CanBePaved && _pavingMode)
            {
                _pavingMode = false;
                _view.SetPavingMode(false);
            }
            _view.SetModeSwitchAvailable(_bridge.Data.CanBePaved);

            RefreshSegmentDisplay();
        }

        private void RefreshSegmentDisplay()
        {
            if (_bridge == null)
            {
                _view.ShowBridge(null, false, null);
                return;
            }

            if (_pavingMode)
            {
                _view.ShowBridge(_bridge, true, null);
                _view.ShowPavements(_bridge, _bridge.GetPavements());
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
            RefreshSegmentDisplay();
        }

        private void OnPavingSelected(int choiceIndex)
        {
            _selectedPaving = _pavingChoices[choiceIndex];
        }

        private void OnPavementsChanged()
        {
            if (_pavingMode && _bridge != null)
            {
                _view.ShowPavements(_bridge, _bridge.GetPavements());
            }
        }

        private void OnApplyToAllClicked()
        {
            if (_bridge == null || !_bridge.Data.CanBePaved)
            {
                return;
            }

            BridgePavementData[] oldPavements = _bridge.GetPavements();
            BridgePavementData[] newPavements = oldPavements.Select(_ => _selectedPaving).ToArray();
            if (newPavements.SequenceEqual(oldPavements))
            {
                return;
            }

            ExecutePavingCommand(oldPavements, newPavements);
        }

        private void OnSegmentClicked(int index)
        {
            if (_pavingMode)
            {
                OnPavingSegmentClicked(index);
                return;
            }

            if (!_editable || _bridge == null || _pendingSupports == null
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

        private void OnPavingSegmentClicked(int index)
        {
            if (_bridge == null || !_bridge.Data.CanBePaved || index < 0 || index >= _bridge.SegmentCount)
            {
                return;
            }

            BridgePavementData[] oldPavements = _bridge.GetPavements();
            if (oldPavements[index] == _selectedPaving)
            {
                return;
            }

            BridgePavementData[] newPavements = (BridgePavementData[])oldPavements.Clone();
            newPavements[index] = _selectedPaving;
            ExecutePavingCommand(oldPavements, newPavements);
        }

        private void ExecutePavingCommand(BridgePavementData[] oldPavements, BridgePavementData[] newPavements)
        {
            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(
                new BridgePavingChangeCommand(_bridge, oldPavements, newPavements));
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
