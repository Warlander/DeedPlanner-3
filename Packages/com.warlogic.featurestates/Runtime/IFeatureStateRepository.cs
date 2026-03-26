using System;

namespace Warlogic.Features
{
    public interface IFeatureStateRepository<TFeature> where TFeature : Enum
    {
        bool IsEnabledInProduction(TFeature feature);
        bool IsEnabledInDebug(TFeature feature);
        bool IsEnabledInEditor(TFeature feature);
    }
}
