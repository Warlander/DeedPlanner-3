using System.Threading.Tasks;

namespace Warlander.Deedplanner.Persistence
{
    public interface IAutoSaveScheduler
    {
        Task AutoSaveNowAsync();
        Task<SavedMapInfo?> FindRecoverySlotAsync(MapLocation mainLocation);
        Task<SavedMapInfo?> FindNeverSavedRecoveryAsync();
        Task DeleteSlotsAsync(MapLocation mainLocation);
    }
}
