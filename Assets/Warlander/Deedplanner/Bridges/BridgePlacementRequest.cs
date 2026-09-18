using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Bridges
{
    public readonly struct BridgePlacementRequest
    {
        public TileCoords Start { get; }
        public TileCoords End { get; }
        public BridgeData Material { get; }
        public BridgeType Type { get; }
        public int AdditionalData { get; }
        public string Segments { get; }

        public BridgePlacementRequest(TileCoords start, TileCoords end, BridgeData material, BridgeType type,
            int additionalData, string segments)
        {
            Start = start;
            End = end;
            Material = material;
            Type = type;
            AdditionalData = additionalData;
            Segments = segments;
        }
    }
}
