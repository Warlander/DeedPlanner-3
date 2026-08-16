namespace Warlander.Deedplanner.Data.Bridges
{
    public readonly struct BridgeOption
    {
        public BridgeData Data { get; }
        public BridgeType Type { get; }
        public int ExtraArgument { get; }

        public BridgeOption(BridgeData data, BridgeType type, int extraArgument)
        {
            Data = data;
            Type = type;
            ExtraArgument = extraArgument;
        }
    }
}
