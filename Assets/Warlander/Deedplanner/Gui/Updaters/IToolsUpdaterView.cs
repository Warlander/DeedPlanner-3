using System;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IToolsUpdaterView
    {
        event Action<ToolsMode> ModeChanged;
        event Action<ToolsMaterialsScope> MaterialsScopeChanged;
        event Action MaterialsCalculationRequested;

        void ShowPanel(ToolsMode mode);
        void ClearWarnings();
        void AddWarning(string text);
    }
}
