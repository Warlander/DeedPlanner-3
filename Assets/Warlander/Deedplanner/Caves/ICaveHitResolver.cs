namespace Warlander.Deedplanner.Caves
{
    public interface ICaveHitResolver
    {
        bool TryResolve(CaveChunk chunk, int triangleIndex, out CaveHit hit);
        bool IsCurrent(CaveHit hit);
    }
}
