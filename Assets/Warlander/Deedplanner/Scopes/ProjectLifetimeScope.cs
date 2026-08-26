using UnityEngine;
using UnityEngine.CrashReportHandler;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Features;
using Warlogic.Features;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logging;
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

            var loggerSource = new LoggerSource(new LogLevelFilter());
            builder.RegisterInstance(loggerSource);
            builder.RegisterInstance<ILoggerSource>(loggerSource);
            builder.RegisterEntryPoint<LoggingConfigurator>();

            // Disable exception reporting as soon as possible if in editor,
            // before any other code could throw an exception.
            // We do this to prevent crash reporting bad data.
            if (Application.isEditor)
            {
                CrashReportHandler.enableCaptureExceptions = false;
            }

            builder.RegisterInstance(new SettingsFactory(loggerSource).Create());

#if DISABLESTEAMWORKS
            builder.RegisterEntryPoint<DummySteamConnection>();
#else
            builder.RegisterEntryPoint<SteamConnection>();
#endif

            builder.RegisterEntryPoint<DefaultTargetFrameRateSetter>();

            var sharedMaterials = Resources.Load<SharedMaterials>("SharedMaterials");
            builder.RegisterInstance(sharedMaterials).As<ISharedMaterials>();

            builder.Register<WurmAssetFacade>(Lifetime.Singleton).As<IWurmAssetFacade>();

            builder.RegisterInstance(new ResourceFeatureStateRepositoryRetriever<Feature>("FeatureStates").Get());
            builder.Register<FeatureStateRetriever<Feature>>(Lifetime.Singleton).As<IFeatureStateRetriever<Feature>>();
        }
    }
}
