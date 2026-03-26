using System;

namespace Warlogic.Features
{
    public interface IFeatureStateRetriever<TFeature> where TFeature : Enum
    {
        bool IsFeatureEnabled(TFeature feature);
    }
}
