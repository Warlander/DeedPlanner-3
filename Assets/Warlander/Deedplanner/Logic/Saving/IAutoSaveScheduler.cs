using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic.Saving
{
    public interface IAutoSaveScheduler
    {
        Task AutoSaveNowAsync();
        Task<MapLocation?> FindRecoverySlotAsync(MapLocation mainLocation);
        Task<MapLocation?> FindNeverSavedRecoveryAsync();
        Task DeleteSlotsAsync(MapLocation mainLocation);
    }
}
