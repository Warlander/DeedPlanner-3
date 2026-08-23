using System;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Floors;

namespace Warlander.Deedplanner.Data.Docks
{
    public class DockFactory
    {
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

                return CreateDock(map, x, y, height, floor, support, braceRotation);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Unable to load dock: " + e.Message);
                return null;
            }
        }

        public Dock CreateDock(Map map, int x, int y, int height, FloorData floor, DockSupportData support,
            EntityOrientation braceRotation)
        {
            GameObject dockObject = new GameObject("Dock " + floor.Name, typeof(Dock));
            Dock dock = dockObject.GetComponent<Dock>();
            map[x, y].RegisterDock(dock);
            dock.Initialize(map[x, y], height, floor, support, braceRotation);
            return dock;
        }
    }
}
