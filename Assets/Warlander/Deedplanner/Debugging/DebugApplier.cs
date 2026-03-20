using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Graphics.Projectors;
using Warlander.Deedplanner.Logic;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Debugging
{
    public class DebugApplier : IInitializable, ITickable
    {
        [Inject] private DebugProperties _debugProperties;
        [Inject] private IMapProjectorFacade _mapProjectorFacade;
        [Inject] private TabContext _tabContext;

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
                foreach (KeyValuePair<string,DecorationData> pair in Database.Decorations)
                {
                    DecorationData data = pair.Value;
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