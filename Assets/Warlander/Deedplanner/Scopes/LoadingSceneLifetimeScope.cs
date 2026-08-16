using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Scopes;

namespace Warlander.Deedplanner.Scopes
{
    public class LoadingSceneLifetimeScope : CommonSceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // TODO: Refactor this. Injecting dependencies into GO's isn't recommended, injection patterns around GO's should be done VContainer way instead of Zenject way - inject GO's into C# classes only, never other way around.
            builder.RegisterBuildCallback(container =>
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        container.InjectGameObject(mb.gameObject);
                    }
                }
            });
            
            base.Configure(builder);

            builder.RegisterEntryPoint<DataLoader>().AsSelf();
        }
    }
}
