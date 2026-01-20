using System;

namespace Warlander.Deedplanner.Settings
{
    public interface IMapRenderSettingsRetriever
    {
        event Action Changed;

        bool RenderDecorations { get; }
        bool RenderTrees { get; }
        bool RenderBushes { get; }
        bool RenderShips { get; }
    }
}