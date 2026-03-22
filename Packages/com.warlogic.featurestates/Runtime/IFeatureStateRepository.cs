namespace Warlogic.Features
{
    public interface IFeatureStateRepository
    {
        bool IsEnabledInProduction(string featureName);
        bool IsEnabledInDebug(string featureName);
        bool IsEnabledInEditor(string featureName);
    }
}