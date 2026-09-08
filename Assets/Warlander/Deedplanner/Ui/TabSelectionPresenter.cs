using Warlander.Deedplanner.Editing;
using System;
using VContainer.Unity;

namespace Warlander.Deedplanner.Ui
{
    public class TabSelectionPresenter : IInitializable, IDisposable
    {
        private readonly ITabSelectionView _view;
        private readonly TabContext _tabContext;

        public TabSelectionPresenter(ITabSelectionView view, TabContext tabContext)
        {
            _view = view;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            _view.TabSelected += OnTabSelected;
            _tabContext.TabChanged += OnTabChanged;
            _view.SelectTab(_tabContext.CurrentTab);
        }

        public void Dispose()
        {
            _view.TabSelected -= OnTabSelected;
            _tabContext.TabChanged -= OnTabChanged;
        }

        private void OnTabSelected(Tab tab)
        {
            _tabContext.CurrentTab = tab;
        }

        private void OnTabChanged(Tab tab)
        {
            _view.SelectTab(tab);
        }
    }
}
