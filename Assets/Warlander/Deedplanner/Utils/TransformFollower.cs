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

            Transform currentTransform = transform;
            Vector3 currentTransformPosition = currentTransform.position;
            Vector3 followedTransformPosition = FollowedTransform.position;

            float x = FollowX ? followedTransformPosition.x : currentTransformPosition.x;
            float y = FollowY ? followedTransformPosition.y : currentTransformPosition.y;
            float z = FollowZ ? followedTransformPosition.z : currentTransformPosition.z;
            currentTransform.position = new Vector3(x, y, z);
        }

    }
}
