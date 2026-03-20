using System;
using System.Collections.Generic;
using VContainer.Unity;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui
{
    public class WindowOpenerPresenter : IInitializable, IDisposable
    {
        private readonly IReadOnlyList<IWindowOpenerButtonView> _views;
        private readonly WindowCoordinator _windowCoordinator;

        public WindowOpenerPresenter(IReadOnlyList<IWindowOpenerButtonView> views, WindowCoordinator windowCoordinator)
        {
            _views = views;
            _windowCoordinator = windowCoordinator;
        }

        public void Initialize()
        {
            foreach (IWindowOpenerButtonView view in _views)
                view.WindowOpenRequested += OnWindowOpenRequested;
        }

        public void Dispose()
        {
            foreach (IWindowOpenerButtonView view in _views)
                view.WindowOpenRequested -= OnWindowOpenRequested;
        }

        private void OnWindowOpenRequested(WindowOpenRequest request)
        {
            if (request.Exclusive)
                _windowCoordinator.CreateWindowExclusive(request.WindowName);
            else
                _windowCoordinator.CreateWindow(request.WindowName);
        }
    }
}
