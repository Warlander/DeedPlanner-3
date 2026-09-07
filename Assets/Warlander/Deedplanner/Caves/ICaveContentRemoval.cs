namespace Warlander.Deedplanner.Caves
{
    public interface ICaveContentRemoval
    {
        void Remove();
        void Restore();
        void DestroyRemoved();
    }
}
