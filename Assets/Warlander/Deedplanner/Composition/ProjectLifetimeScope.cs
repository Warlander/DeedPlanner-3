using UnityEngine;
using UnityEngine.CrashReportHandler;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Platform.Features;
using Warlogic.Features;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Platform.Steam;
using Warlander.Scopes;
using Warlogic.Settings;
using Warlander.Deedplanner.Inputs;

namespace Warlander.Deedplanner.Composition
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

            var settings = DeedPlannerSettings.Create(loggerSource.Create(DeedPlannerSettings.Category));
            builder.RegisterInstance(settings.Registry);
            builder.RegisterInstance<ISettingsStore>(settings.Store);
            builder.RegisterInstance(settings.Camera);
            builder.RegisterInstance(settings.Editing);
            builder.RegisterInstance(settings.Ui);
            builder.RegisterInstance(settings.Graphics);
            builder.RegisterEntryPoint<QualityLevelApplier>();

            builder.RegisterInstance(new DPInput());
            builder.RegisterEntryPoint<InputSettings>().AsSelf();
            builder.RegisterEntryPoint<KeybindSettingsRegistrar>();

            builder.Register<Database>(Lifetime.Singleton).AsSelf().As<IDataCatalog>();

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
