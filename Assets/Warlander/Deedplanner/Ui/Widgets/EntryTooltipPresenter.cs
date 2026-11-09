using System;
using VContainer.Unity;
using Warlander.Deedplanner.Ui.Tooltips;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public class EntryTooltipPresenter : IInitializable, IDisposable
    {
        private readonly IEntryTooltipView _view;
        private readonly TooltipHandler _tooltipHandler;

        public EntryTooltipPresenter(IEntryTooltipView view, TooltipHandler tooltipHandler)
        {
            _view = view;
            _tooltipHandler = tooltipHandler;
        }

        public void Initialize()
        {
            _view.TooltipRequested += OnTooltipRequested;
        }

        public void Dispose()
        {
            _view.TooltipRequested -= OnTooltipRequested;
        }

        private void OnTooltipRequested(string text)
        {
            _tooltipHandler.ShowTooltipText(text);
        }
    }
}
