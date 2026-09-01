using Warlander.Deedplanner.Editing;
using System;

namespace Warlander.Deedplanner.Ui
{
    public interface ITabSelectionView
    {
        event Action<Tab> TabSelected;
    }
}
