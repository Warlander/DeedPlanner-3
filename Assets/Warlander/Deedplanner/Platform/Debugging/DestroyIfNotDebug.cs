using UnityEngine;

namespace Warlander.Deedplanner.Platform.Debugging
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
