namespace Warlander.Deedplanner.Gui
{
    public readonly struct WindowOpenRequest
    {
        public string WindowName { get; }
        public bool Exclusive { get; }

        public WindowOpenRequest(string windowName, bool exclusive)
        {
            WindowName = windowName;
            Exclusive = exclusive;
        }
    }
}
