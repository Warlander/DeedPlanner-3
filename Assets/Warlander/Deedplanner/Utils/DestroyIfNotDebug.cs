using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class DestroyIfNotDebug : MonoBehaviour
    {
        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                Destroy(gameObject);
            }
        }
    }
}