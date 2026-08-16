using Plugins.Warlander.Utils;
using VContainer;
using VContainer.Unity;

namespace Warlander.Scopes
{
    /// <summary>
    /// Add this to VContainer root LifetimeScope of the project.
    /// </summary>
    public class CommonProjectScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentOnNewGameObject<UnityThreadRunner>(Lifetime.Singleton, "UnityThreadRunner");
        }
    }
}
