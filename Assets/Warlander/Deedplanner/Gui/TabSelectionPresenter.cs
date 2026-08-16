using System;
using VContainer.Unity;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui
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
        }

        public void Dispose()
        {
            _view.TabSelected -= OnTabSelected;
        }

        private void OnTabSelected(Tab tab)
        {
            _tabContext.CurrentTab = tab;
        }
    }
}
