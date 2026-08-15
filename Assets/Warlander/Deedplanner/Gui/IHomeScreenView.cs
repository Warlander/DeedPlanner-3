using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Gui
{
    public enum HomeScreenChip
    {
        None, Missing, Unknown, Volatile, Recovery
    }

    public readonly struct HomeScreenCategory
    {
        public readonly string BackendId;
        public readonly string Label;

        public HomeScreenCategory(string backendId, string label)
        {
            BackendId = backendId;
            Label = label;
        }
    }

    public readonly struct HomeScreenCardData
    {
        public readonly MapLocation Location;
        public readonly string Name;
        public readonly string TimeText;
        public readonly string LocationHint;
        public readonly string BadgeText;
        public readonly Texture2D Thumbnail;
        public readonly HomeScreenChip Chip;

        public HomeScreenCardData(MapLocation location, string name, string timeText,
            string locationHint, string badgeText, Texture2D thumbnail, HomeScreenChip chip)
        {
            Location = location;
            Name = name;
            TimeText = timeText;
            LocationHint = locationHint;
            BadgeText = badgeText;
            Thumbnail = thumbnail;
            Chip = chip;
        }
    }

    public interface IHomeScreenView
    {
        event Action BackClicked;
        event Action NewDeedClicked;
        event Action LoadClicked;
        event Action WebLinkClicked;
        event Action AboutClicked;
        event Action QuitClicked;
        event Action<string> CategoryClicked;
        event Action<MapLocation> CardClicked;

        bool Visible { get; }
        void Show();
        void Hide();
        void SetLoadButtonVisible(bool visible);
        void SetCategories(IReadOnlyList<HomeScreenCategory> categories, string selectedBackendId);
        void SetCards(IReadOnlyList<HomeScreenCardData> cards);
        void UpdateCard(MapLocation location, HomeScreenCardData data);
    }
}
