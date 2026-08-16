using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic.Saving
{
    public interface IAutoSaveScheduler
    {
        Task AutoSaveNowAsync();
        Task<SavedMapInfo?> FindRecoverySlotAsync(MapLocation mainLocation);
        Task<SavedMapInfo?> FindNeverSavedRecoveryAsync();
        Task DeleteSlotsAsync(MapLocation mainLocation);
    }
}
