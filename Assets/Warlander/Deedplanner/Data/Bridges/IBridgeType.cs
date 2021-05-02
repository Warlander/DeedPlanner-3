namespace Warlander.Deedplanner.Data.Bridges
{
    public interface IBridgeType
    {
        string Name { get; }
        int[] ExtraArguments { get; }
        BridgeType Type { get; }
        int CalculateAddedHeight(int currentSegment, int bridgeLength, int startHeight, int endHeight, int extraArgument);
    }
}