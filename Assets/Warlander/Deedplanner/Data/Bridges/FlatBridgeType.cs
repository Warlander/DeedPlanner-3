namespace Warlander.Deedplanner.Data.Bridges
{
    public class FlatBridgeType : IBridgeType
    {
        public string Name => "flat";
        public int[] ExtraArguments { get; } = { 0 };
        
        public int CalculateAddedHeight(int currentSegment, int bridgeLength, int startHeight, int endHeight, int extraArgument)
        {
            return 0;
        }
    }
}