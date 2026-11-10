namespace Warlander.Deedplanner.Bridges
{
    public sealed class BridgeShapeState
    {
        public BridgeType Type { get; }
        public string Segments { get; }
        public int AdditionalData { get; }
        public int? RememberedArchSteepness { get; }

        private readonly bool[] _flatSupports;

        public BridgeShapeState(BridgeType type, string segments, int additionalData,
            bool[] flatSupports, int? rememberedArchSteepness)
        {
            Type = type;
            Segments = segments;
            AdditionalData = additionalData;
            _flatSupports = flatSupports == null ? null : (bool[])flatSupports.Clone();
            RememberedArchSteepness = rememberedArchSteepness;
        }

        public bool[] CopyFlatSupports() => _flatSupports == null ? null : (bool[])_flatSupports.Clone();
    }
}
