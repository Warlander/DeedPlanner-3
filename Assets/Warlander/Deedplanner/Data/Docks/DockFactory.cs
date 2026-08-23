using System;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Graphics;
using VContainer;

namespace Warlander.Deedplanner.Data.Docks
{
    public class DockFactory
    {
        private readonly ISharedMaterials _sharedMaterials;

        [Inject]
        public DockFactory(ISharedMaterials sharedMaterials)
        {
            _sharedMaterials = sharedMaterials;
        }

        public Dock CreateDock(Map map, XmlElement element)
        {
            try
            {
                int x = int.Parse(element.GetAttribute("x"));
                int y = int.Parse(element.GetAttribute("y"));
                int height = int.Parse(element.GetAttribute("height"));

                string floorId = element.GetAttribute("floor");
                if (!Database.Floors.TryGetValue(floorId, out FloorData floor))
                {
                    Debug.LogWarning("Unable to load dock: unknown floor " + floorId);
                    return null;
                }

                string supportId = element.GetAttribute("support");
                DockSupportData support = null;
                if (supportId != "none" && !Database.DockSupports.TryGetValue(supportId, out support))
                {
                    Debug.LogWarning("Unable to load dock: unknown support " + supportId);
                    return null;
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
                Debug.LogWarning("Unable to load dock: " + e.Message);
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
