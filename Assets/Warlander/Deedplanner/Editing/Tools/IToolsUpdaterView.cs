using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public interface IToolsUpdaterView
    {
        event Action<ToolsMode> ModeChanged;
        event Action<ToolsMaterialsScope> MaterialsScopeChanged;
        event Action MaterialsCalculationRequested;

        void ShowPanel(ToolsMode mode);
        void ClearWarnings();
        void AddWarning(string text);
        void ShowAreaSelection(RectTransform screen, IReadOnlyList<Vector2> segments, Vector2Int dimensions);
        void HideAreaSelection();
    }
}
