using System.ComponentModel;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class LoadingSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DataLoader>().AsSingle().NonLazy();
        }
    }
}