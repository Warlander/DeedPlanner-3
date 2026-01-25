namespace Warlander.Deedplanner.Features
{
    public interface IFeatureStateRetriever
    {
        bool IsFeatureEnabled(Feature feature);
    }
}
