using UnityEngine;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Rendering.Projectors;
using Warlander.Deedplanner.Logic;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Platform.Debugging
{
    public class DebugApplier : IInitializable, ITickable
    {
        [Inject] private DebugProperties _debugProperties;
        [Inject] private IMapProjectorFacade _mapProjectorFacade;
        [Inject] private TabContext _tabContext;
        [Inject] private IDataCatalog _dataCatalog;

        void IInitializable.Initialize()
        {
            if (_debugProperties.DrawDebugPlaneLines)
            {
                IMapProjector horizontalLine = _mapProjectorFacade.RequestProjector(ProjectorColor.Green);
                horizontalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Horizontal);
                IMapProjector firstVerticalLine = _mapProjectorFacade.RequestProjector(ProjectorColor.Red);
                firstVerticalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Vertical);
                IMapProjector secondVerticalLine = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                secondVerticalLine.ProjectLine(new Vector2Int(15, 15), PlaneAlignment.Vertical);
            }

            if (_debugProperties.PreloadAllDecorations)
            {
                foreach (DecorationData data in _dataCatalog.GetAllDecorations())
                {
                    data.Model.CreateOrGetModel(GameObject.Destroy);
                }
            }
        }

        void ITickable.Tick()
        {
            if (_debugProperties.OverrideStartingTileSelectionMode)
            {
                _tabContext.TileSelectionMode = _debugProperties.TileSelectionMode;
            }
        }
    }
}
