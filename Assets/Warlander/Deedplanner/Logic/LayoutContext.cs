using System;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
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
