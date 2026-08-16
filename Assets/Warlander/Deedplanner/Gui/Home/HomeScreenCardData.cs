using UnityEngine;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Gui.Home
{
    public readonly struct HomeScreenCardData
    {
        public readonly MapLocation Location;
        public readonly string Name;
        public readonly string TimeText;
        public readonly string LocationHint;
        public readonly string BadgeText;
        public readonly Texture2D Thumbnail;
        public readonly HomeScreenChip Chip;
        public readonly bool ShowDelete;

        public HomeScreenCardData(MapLocation location, string name, string timeText,
            string locationHint, string badgeText, Texture2D thumbnail, HomeScreenChip chip,
            bool showDelete = true)
        {
            Location = location;
            Name = name;
            TimeText = timeText;
            LocationHint = locationHint;
            BadgeText = badgeText;
            Thumbnail = thumbnail;
            Chip = chip;
            ShowDelete = showDelete;
        }
    }
}
