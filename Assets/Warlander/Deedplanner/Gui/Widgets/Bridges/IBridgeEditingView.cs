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
        event Action<int> SelectedExtraArgumentChanged;

        bool IsActive { get; }

        void SetDeleteButtonVisible(bool visible);
        void SetCancelButtonVisible(bool visible);
        void SetTypeLabel(string text);
        void SetMaterials(IReadOnlyList<BridgeData> materials, int selectedIndex);
        void SetMaterialsVisible(bool visible);
        void SetExtraArguments(IReadOnlyList<int> values, int selectedIndex);
        void SetExtraArgumentsVisible(bool visible);
    }
}
