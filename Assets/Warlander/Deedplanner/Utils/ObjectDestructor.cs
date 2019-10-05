using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class ObjectDestructor : MonoBehaviour
    {

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
        
    }
}