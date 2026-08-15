using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic.Saving
{
    public interface ISaveBackend
    {
        string Id { get; }
        string DisplayName { get; }
        SaveCapabilities Capabilities { get; }
        bool IsVolatile { get; }
        bool CompressesOutput { get; }

        SaveFeasibility CheckSave(long payloadBytes);

        /// Full save flow including any location picker. Returns null when cancelled.
        Task<MapLocation?> SaveAsync(string payload, string suggestedName);

        /// Overwrites a previously saved location. Only valid with the Overwrite capability.
        Task OverwriteAsync(MapLocation target, string payload);

        /// Picks a location to load from. Only valid with the Track capability. Returns null when cancelled.
        Task<MapLocation?> PickLoadLocationAsync();

        Task<string> LoadAsync(MapLocation source);

        /// Only valid with the Track capability.
        Task<TrackResult> TrackAsync(MapLocation target);
    }
}
