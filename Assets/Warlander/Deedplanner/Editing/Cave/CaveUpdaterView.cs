using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class CaveUpdaterView : MonoBehaviour, ICaveUpdaterView
    {
        [SerializeField] private UnityTree _cavesTree;

        public void AddCaveEntry(CaveData data, string[] category)
        {
            _cavesTree.Add(data, category);
        }
    }
}
