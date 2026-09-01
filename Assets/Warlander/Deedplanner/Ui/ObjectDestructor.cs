using UnityEngine;

namespace Warlander.Deedplanner.Ui
{
    public class ObjectDestructor : MonoBehaviour
    {
        public void DestroyObject()
        {
            Destroy(gameObject);
        }
    }
}
