using System;
using System.Collections.Generic;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public interface IBridgeCreationView
    {
        event Action<BridgeData> SelectedMaterialChanged;
        event Action<BridgeType?> SelectedTypeChanged;
        event Action PlaceClicked;
        event Action CancelClicked;
        event Action BecameActive;
        event Action BecameInactive;

        BridgeData SelectedMaterial { get; }
        BridgeType? SelectedType { get; }
        bool IsActive { get; }

        void SetMaterials(IReadOnlyList<BridgeData> materials, int selectedIndex = 0);
        void SetTypes(IReadOnlyList<BridgeType> types, bool visible, int selectedIndex = 0);
        void SetPlaceButtonVisible(bool visible);
        void SetCancelButtonVisible(bool visible);
        void SetMessage(string message);
    }
}
