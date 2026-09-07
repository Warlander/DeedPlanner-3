namespace Warlander.Deedplanner.Domain.Entities.Grounds
{
    public interface IGroundDataResolver
    {
        GroundData Resolve(string shortName);
    }
}
