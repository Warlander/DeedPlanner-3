using System;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui
{
    public interface ITabSelectionView
    {
        event Action<Tab> TabSelected;
    }
}
