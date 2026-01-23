using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmModelFactory
    {
        Model CreateModel(XmlElement element, Vector3 scale, int layer = int.MaxValue);
        Model CreateModel(XmlElement element, int layer = int.MaxValue);
        Model CreateModel(string location, Vector3 scale, int layer = int.MaxValue);
        Model CreateModel(string newLocation, int layer = int.MaxValue);
    }
}
