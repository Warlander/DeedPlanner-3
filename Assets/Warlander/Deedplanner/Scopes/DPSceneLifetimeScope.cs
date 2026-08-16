using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Scopes;

namespace Warlander.Deedplanner.Scopes
{
    public class DPSceneLifetimeScope : CommonSceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterEntryPoint<TooltipHandler>().AsSelf();
        }
    }
}
