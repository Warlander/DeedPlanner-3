using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic.Projectors;
using Zenject;

namespace Warlander.Deedplanner.Debugging
{
    public class DebugApplier : IInitializable, ITickable
    {
        [Inject] private DebugProperties _debugProperties;
        [Inject] private MapProjectorManager _mapProjectorManager;
        
        void IInitializable.Initialize()
        {
            if (_debugProperties.DrawDebugPlaneLines)
            {
                MapProjector horizontalLine = _mapProjectorManager.RequestProjector(ProjectorColor.Green);
                horizontalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Horizontal);
                MapProjector firstVerticalLine = _mapProjectorManager.RequestProjector(ProjectorColor.Red);
                firstVerticalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Vertical);
                MapProjector secondVerticalLine = _mapProjectorManager.RequestProjector(ProjectorColor.Yellow);
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
                LayoutManager.Instance.TileSelectionMode = _debugProperties.TileSelectionMode;
            }
        }
    }
}