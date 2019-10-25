using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public abstract class AbstractUpdater : MonoBehaviour
    {
        [SerializeField] private Tab targetTab = Tab.Ground;

        public Tab TargetTab => targetTab;
    }
}