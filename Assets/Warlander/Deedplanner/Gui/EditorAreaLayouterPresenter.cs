using System;
using VContainer.Unity;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Gui
{
    public class EditorAreaLayouterPresenter : IInitializable, IDisposable
    {
        private readonly IEditorAreaLayouterView _view;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly LayoutContext _layoutContext;

        public EditorAreaLayouterPresenter(
            IEditorAreaLayouterView view,
            CameraCoordinator cameraCoordinator,
            LayoutContext layoutContext)
        {
            _view = view;
            _cameraCoordinator = cameraCoordinator;
            _layoutContext = layoutContext;
        }

        public void Initialize()
        {
            _layoutContext.LayoutChanged += OnLayoutChanged;
            OnLayoutChanged(_layoutContext.CurrentLayout);
        }

        public void Dispose()
        {
            _layoutContext.LayoutChanged -= OnLayoutChanged;
        }

        private void OnLayoutChanged(Layout layout)
        {
            switch (layout)
            {
                case Layout.Single:
                    ApplyVisibility(true, false, false, false);
                    break;
                case Layout.HorizontalSplit:
                    ApplyVisibility(true, false, true, false);
                    break;
                case Layout.VerticalSplit:
                    ApplyVisibility(true, true, false, false);
                    break;
                case Layout.HorizontalTop:
                    ApplyVisibility(true, false, true, true);
                    break;
                case Layout.HorizontalBottom:
                    ApplyVisibility(true, true, true, false);
                    break;
                case Layout.Quad:
                    ApplyVisibility(true, true, true, true);
                    break;
            }
        }

        private void ApplyVisibility(bool topRight, bool topLeft, bool bottomRight, bool bottomLeft)
        {
            ToggleScreen(0, topRight);
            ToggleScreen(1, topLeft);
            ToggleScreen(2, bottomRight);
            ToggleScreen(3, bottomLeft);

            bool bottomRowVisible = bottomRight || bottomLeft;
            _view.SetBottomRowVisible(bottomRowVisible);

            bool onlyOneTopActive = topRight ^ topLeft;
            bool onlyOneBottomActive = bottomRight ^ bottomLeft;
            bool onlyOneLeftActive = topLeft ^ bottomLeft;
            bool onlyOneRightActive = topRight ^ bottomRight;

            // "-" shaped split
            _view.SetSplitVisible(0, onlyOneTopActive && onlyOneBottomActive);
            // "|" shaped split
            _view.SetSplitVisible(1, onlyOneLeftActive && onlyOneRightActive);
            // "T" shaped split
            _view.SetSplitVisible(2, onlyOneTopActive && bottomRight && bottomLeft);
            // Reverse "T" shaped split
            _view.SetSplitVisible(3, topRight && topLeft && onlyOneBottomActive);
            // "+" shaped split
            _view.SetSplitVisible(4, topRight && topLeft && bottomRight && bottomLeft);
        }

        private void ToggleScreen(int index, bool enable)
        {
            _cameraCoordinator.ToggleCamera(index, enable);
            _view.SetScreenVisible(index, enable);

            // if screen is being toggled off, focus primary screen instead
            if (!enable && _cameraCoordinator.ActiveId == index)
            {
                _cameraCoordinator.ChangeCurrentCamera(0);
            }
        }
    }
}
