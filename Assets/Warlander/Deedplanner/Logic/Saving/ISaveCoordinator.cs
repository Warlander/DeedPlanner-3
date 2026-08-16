using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic.Saving
{
    public interface ISaveCoordinator
    {
        MapLocation? CurrentLocation { get; }
        DateTime? LastSaveTimeUtc { get; }
        bool Busy { get; }
        IReadOnlyList<ISaveBackend> Backends { get; }
        RecentMapsStore RecentMaps { get; }

        event Action SaveStateChanged;

        ISaveBackend GetBackend(string id);
        bool CanQuickSave { get; }

        string SerializeCurrentMap();
        string SerializeCurrentMap(out byte[] thumbnailJpeg);

        Task<MapLocation?> SaveAsync(string backendId);
        Task<bool> QuickSaveAsync();
        Task<bool> PickAndLoadAsync(string backendId);
        Task<bool> LoadAsync(MapLocation location);
        Task<bool> LoadFromWebAsync(string rawLink);
        Task<bool> LoadRecoveryAsync(MapLocation slot, MapLocation? mainLocation);
        Task NewMapAsync(int width = 25, int height = 25);
        Task DeleteSaveAsync(MapLocation location);

        Task AutoSaveToAsync(MapLocation slot);
        Task<byte[]> ReadThumbnailAsync(MapLocation location);

        Task ResizeMapAsync(int left, int right, int bottom, int top);
        Task ClearMapAsync();
        Task PrepareForQuitAsync();
    }
}
