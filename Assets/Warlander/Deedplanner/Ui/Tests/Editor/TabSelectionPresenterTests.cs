using System;
using NUnit.Framework;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class TabSelectionPresenterTests
    {
        [Test]
        public void TabContextChangeSelectsMatchingViewToggle()
        {
            var view = new TabSelectionViewSpy();
            var context = new TabContext(null) { CurrentTab = Tab.Ground };
            var presenter = new TabSelectionPresenter(view, context);
            presenter.Initialize();

            context.CurrentTab = Tab.Caves;

            Assert.That(view.SelectedTab, Is.EqualTo(Tab.Caves));
            presenter.Dispose();
        }

        [Test]
        public void ViewSelectionChangesTabContext()
        {
            var view = new TabSelectionViewSpy();
            var context = new TabContext(null) { CurrentTab = Tab.Ground };
            var presenter = new TabSelectionPresenter(view, context);
            presenter.Initialize();

            view.Select(Tab.Caves);

            Assert.That(context.CurrentTab, Is.EqualTo(Tab.Caves));
            presenter.Dispose();
        }

        private sealed class TabSelectionViewSpy : ITabSelectionView
        {
            public event Action<Tab> TabSelected;

            public Tab SelectedTab { get; private set; }

            public void SelectTab(Tab tab)
            {
                SelectedTab = tab;
            }

            public void Select(Tab tab)
            {
                TabSelected?.Invoke(tab);
            }
        }
    }
}
