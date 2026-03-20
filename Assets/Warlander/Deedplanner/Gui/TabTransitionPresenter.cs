using System;
using VContainer.Unity;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui
{
    public class TabTransitionPresenter : IInitializable, IDisposable
    {
        private readonly ITabTransitionView _view;
        private readonly TabContext _tabContext;

        public TabTransitionPresenter(ITabTransitionView view, TabContext tabContext)
        {
            _view = view;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            _tabContext.TabChanged += OnTabChanged;
            _view.ShowTab(_tabContext.CurrentTab, animated: false);
        }

        public void Dispose()
        {
            _tabContext.TabChanged -= OnTabChanged;
        }

        private void OnTabChanged(Tab tab)
        {
            _view.ShowTab(tab, animated: true);
        }
    }
}
