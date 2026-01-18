namespace Warlander.Deedplanner.Settings
{
    public interface IMapRenderSettingsSetter
    {
        bool RenderDecorations { get; set; }
        bool RenderTrees { get; set; }
        bool RenderBushes { get; set; }
        bool RenderShips { get; set; }
    }
}