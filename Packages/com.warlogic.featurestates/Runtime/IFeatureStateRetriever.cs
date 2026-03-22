namespace Warlogic.Features
{
    public interface IFeatureStateRetriever
    {
        bool IsFeatureEnabled(string featureName);
    }
}
