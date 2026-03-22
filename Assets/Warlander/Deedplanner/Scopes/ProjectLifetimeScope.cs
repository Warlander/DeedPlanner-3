using UnityEngine;
using UnityEngine.CrashReportHandler;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Features;
using Warlogic.Features;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Steam;
using Warlander.Deedplanner.Utils;
using Warlander.Scopes;

namespace Warlander.Deedplanner.Scopes
{
    public class ProjectLifetimeScope : CommonProjectScope
    {
        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // Disable exception reporting as soon as possible if in editor,
            // before any other code could throw an exception.
            // We do this to prevent crash reporting bad data.
            if (Application.isEditor)
            {
                CrashReportHandler.enableCaptureExceptions = false;
            }

            builder.RegisterInstance(new SettingsFactory().Create());

#if DISABLESTEAMWORKS
            builder.RegisterEntryPoint<DummySteamConnection>();
#else
            builder.RegisterEntryPoint<SteamConnection>();
#endif

            builder.RegisterEntryPoint<DefaultTargetFrameRateSetter>();

            var sharedMaterials = Resources.Load<SharedMaterials>("SharedMaterials");
            builder.RegisterInstance(sharedMaterials).As<ISharedMaterials>();

            builder.Register<TextureReferenceFactory>(Lifetime.Singleton).As<ITextureReferenceFactory>();
            builder.Register<WurmMeshLoader>(Lifetime.Singleton).As<IWurmMeshLoader>();
            builder.Register<AggregateTextureLoader>(Lifetime.Singleton).As<ITextureLoader>();
            builder.Register<MaterialLoader>(Lifetime.Singleton).As<IMaterialLoader>();
            builder.Register<MaterialCache>(Lifetime.Singleton).As<IMaterialCache>();
            builder.Register<WurmMaterialLoader>(Lifetime.Singleton).As<IWurmMaterialLoader>();
            builder.Register<WurmModelFactory>(Lifetime.Singleton).As<IWurmModelFactory>();
            builder.Register<BridgePartDataFactory>(Lifetime.Singleton);
            builder.Register<WurmModelLoader>(Lifetime.Singleton).As<IWurmModelLoader>();

            builder.RegisterInstance(new ResourceFeatureStateRepositoryRetriever("FeatureStates").Get());
            builder.Register<FeatureStateRetriever>(Lifetime.Singleton).As<IFeatureStateRetriever>();
        }
    }
}
