using System;

namespace Warlander.Deedplanner.Ui
{
    public class LayoutContext
    {
        public event Action<Layout> LayoutChanged;

        public Layout CurrentLayout { get; private set; } = Layout.Single;

        public void ChangeLayout(Layout layout)
        {
            CurrentLayout = layout;
            LayoutChanged?.Invoke(layout);
        }
    }
}
