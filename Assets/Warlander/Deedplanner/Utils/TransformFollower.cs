using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class TransformFollower : MonoBehaviour
    {

        [SerializeField] private Transform followedTransform = null;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool followZ = true;

        private void Update()
        {
            if (!followedTransform)
            {
                return;
            }

            Transform currentTransform = transform;
            Vector3 currentTransformPosition = currentTransform.position;
            Vector3 followedTransformPosition = followedTransform.position;

            float x = followX ? followedTransformPosition.x : currentTransformPosition.x;
            float y = followY ? followedTransformPosition.y : currentTransformPosition.y;
            float z = followZ ? followedTransformPosition.z : currentTransformPosition.z;
            currentTransform.position = new Vector3(x, y, z);
        }

    }
}
