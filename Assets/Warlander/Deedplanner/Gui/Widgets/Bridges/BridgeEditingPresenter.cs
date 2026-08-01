using System;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Updaters;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeEditingPresenter : IInitializable, IDisposable
    {
        private readonly IBridgeEditingView _view;
        private readonly BridgesUpdater _bridgesUpdater;
        private readonly MapHandler _mapHandler;

        public BridgeEditingPresenter(IBridgeEditingView view, BridgesUpdater bridgesUpdater, MapHandler mapHandler)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
        }

        public void Initialize()
        {
            _view.DeleteClicked += OnDeleteClicked;
            _view.CancelClicked += OnCancelClicked;
            _view.BecameActive += OnViewBecameActive;
            _view.BecameInactive += OnViewBecameInactive;
            _bridgesUpdater.SelectedBridgeChanged += OnSelectedBridgeChanged;

            RefreshActionVisibility();
        }

        public void Dispose()
        {
            _view.DeleteClicked -= OnDeleteClicked;
            _view.CancelClicked -= OnCancelClicked;
            _view.BecameActive -= OnViewBecameActive;
            _view.BecameInactive -= OnViewBecameInactive;
            _bridgesUpdater.SelectedBridgeChanged -= OnSelectedBridgeChanged;
        }

        private void OnSelectedBridgeChanged()
        {
            RefreshActionVisibility();
        }

        private void OnDeleteClicked()
        {
            Bridge selectedBridge = _bridgesUpdater.SelectedBridge;
            if (selectedBridge == null)
            {
                return;
            }

            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(new BridgeRemovalCommand(map, selectedBridge));
            map.CommandManager.FinishAction();
            _bridgesUpdater.ClearBridgeSelection();
        }

        private void OnCancelClicked()
        {
            _bridgesUpdater.ClearBridgeSelection();
        }

        private void OnViewBecameActive()
        {
            RefreshActionVisibility();
        }

        private void OnViewBecameInactive()
        {
            _view.SetDeleteButtonVisible(false);
            _view.SetCancelButtonVisible(false);
        }

        private void RefreshActionVisibility()
        {
            if (!_view.IsActive)
            {
                _view.SetDeleteButtonVisible(false);
                _view.SetCancelButtonVisible(false);
                return;
            }

            bool hasSelection = _bridgesUpdater.SelectedBridge != null;
            _view.SetDeleteButtonVisible(hasSelection);
            _view.SetCancelButtonVisible(true);
        }
    }
}
