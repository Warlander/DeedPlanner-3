using System;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IMenuUpdaterView
    {
        event Action<MenuAction> ButtonClicked;

        void SetQuitButtonVisible(bool visible);
        void SetFullscreenButtonVisible(bool visible);
        void SetVersionText(string text);
        void SetSteamStatus(bool visible, string text);
    }
}
