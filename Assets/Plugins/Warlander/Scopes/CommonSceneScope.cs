using VContainer;
using VContainer.Unity;
using Warlander.UI.Windows;

namespace Warlander.Scopes
{
    /// <summary>
    /// Add this to each scene LifetimeScope.
    /// </summary>
    public class CommonSceneScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<WindowCoordinator>(Lifetime.Singleton);
        }
    }
}
