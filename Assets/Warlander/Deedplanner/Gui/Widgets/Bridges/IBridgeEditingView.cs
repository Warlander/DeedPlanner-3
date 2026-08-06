using System;
using System.Collections.Generic;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public interface IBridgeEditingView
    {
        event Action DeleteClicked;
        event Action CancelClicked;
        event Action BecameActive;
        event Action BecameInactive;
        event Action<BridgeData> SelectedMaterialChanged;

        bool IsActive { get; }

        void SetDeleteButtonVisible(bool visible);
        void SetCancelButtonVisible(bool visible);
        void SetTypeLabel(string text);
        void SetMaterials(IReadOnlyList<BridgeData> materials, int selectedIndex);
        void SetMaterialsVisible(bool visible);
    }
}
