using System;

namespace Warlander.Deedplanner.Logic.Saving
{
    [Flags]
    public enum SaveCapabilities
    {
        Save = 1,
        Load = 2,
        Track = 4,
        Overwrite = 8
    }
}
