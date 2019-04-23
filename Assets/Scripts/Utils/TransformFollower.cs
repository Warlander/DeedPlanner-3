using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class TransformFollower : MonoBehaviour
    {

        public Transform FollowedTransform;
        public bool FollowX;
        public bool FollowY;
        public bool FollowZ;

        private void Update()
        {
            if (!FollowedTransform)
            {
                return;
            }

            float x = FollowX ? FollowedTransform.position.x : transform.position.x;
            float y = FollowY ? FollowedTransform.position.y : transform.position.y;
            float z = FollowZ ? FollowedTransform.position.z : transform.position.z;
            transform.position = new Vector3(x, y, z);
        }

    }
}
