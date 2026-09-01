using Warlander.Deedplanner.Domain;
using System;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logging;
using VContainer;

namespace Warlander.Deedplanner.Docks
{
    public class DockFactory
    {
        public static readonly LogCategory Category = new LogCategory("Docks");

        private readonly ISharedMaterials _sharedMaterials;
        private readonly IDataCatalog _dataCatalog;
        private readonly ICategoryLogger _logger;

        [Inject]
        public DockFactory(ISharedMaterials sharedMaterials, IDataCatalog dataCatalog, ILoggerSource loggerSource)
        {
            _sharedMaterials = sharedMaterials;
            _dataCatalog = dataCatalog;
            _logger = loggerSource.Create(Category);
        }

        public Dock CreateDock(Map map, XmlElement element)
        {
            try
            {
                int x = int.Parse(element.GetAttribute("x"));
                int y = int.Parse(element.GetAttribute("y"));
                int height = int.Parse(element.GetAttribute("height"));

                string floorId = element.GetAttribute("floor");
                FloorData floor = _dataCatalog.GetFloor(floorId);
                if (floor == null)
                {
                    _logger.Warning("Unable to load dock: unknown floor " + floorId);
                    return null;
                }

                string supportId = element.GetAttribute("support");
                DockSupportData support = null;
                if (supportId != "none")
                {
                    support = _dataCatalog.GetDockSupport(supportId);
                    if (support == null)
                    {
                        _logger.Warning("Unable to load dock: unknown support " + supportId);
                        return null;
                    }
                }

                EntityOrientation braceRotation = EntityOrientation.Up;
                if (element.HasAttribute("braceDir"))
                {
                    Enum.TryParse(element.GetAttribute("braceDir"), true, out braceRotation);
                }

                int? anchorLevel = null;
                if (element.HasAttribute("anchorLevel"))
                {
                    anchorLevel = int.Parse(element.GetAttribute("anchorLevel"));
                }

                return CreateDock(map, x, y, height, floor, support, braceRotation, anchorLevel);
            }
            catch (Exception e)
            {
                _logger.Warning("Unable to load dock: " + e.Message);
                return null;
            }
        }

        public Dock CreateDock(Map map, int x, int y, int height, FloorData floor, DockSupportData support,
            EntityOrientation braceRotation, int? anchorLevel = null)
        {
            GameObject dockObject = new GameObject("Dock " + floor.Name, typeof(Dock));
            Dock dock = dockObject.GetComponent<Dock>();
            map[x, y].RegisterDock(dock);
            dock.Initialize(map[x, y], height, floor, support, braceRotation, _sharedMaterials.GhostMaterial, anchorLevel);
            return dock;
        }
    }
}
