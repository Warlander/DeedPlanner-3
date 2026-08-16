using System;
using System.Collections.Generic;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Gui.Home
{
    public interface IHomeScreenView
    {
        event Action BackClicked;
        event Action NewDeedClicked;
        event Action LoadClicked;
        event Action WebLinkClicked;
        event Action AboutClicked;
        event Action QuitClicked;
        event Action<SaveBackendId?> CategoryClicked;
        event Action<MapLocation> CardClicked;
        event Action<MapLocation> CardDeleteClicked;

        bool Visible { get; }
        void Show();
        void Hide();
        void SetLoadButtonVisible(bool visible);
        void SetCategories(IReadOnlyList<HomeScreenCategory> categories, SaveBackendId? selectedBackendId);
        void SetCards(IReadOnlyList<HomeScreenCardData> cards);
        void UpdateCard(MapLocation location, HomeScreenCardData data);
    }
}
