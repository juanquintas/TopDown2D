using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1
{
    public class SmoothCameraFollow : MonoBehaviour
    {
    public Transform target; // The transform the camera should follow
    public Vector3 offset; // World-space offset from the target
    [Range(0.01f, 1f)]
    public float followTime = 0.15f; // Smaller values snap faster
    public float maxFollowSpeed = 50f; // Safety cap for SmoothDamp
    public float snapDistance = 5f; // Recenter instantly if camera drifts too far

    private Vector3 velocity; // Internal SmoothDamp state

        void LateUpdate()
        {
            // Desired position the camera tries to reach
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.z = transform.position.z;

            float planarDistance = Vector3.Distance(new Vector3(transform.position.x, transform.position.y, 0f), new Vector3(desiredPosition.x, desiredPosition.y, 0f));

            if (planarDistance > snapDistance)
            {
                transform.position = desiredPosition;
                velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followTime, maxFollowSpeed, Time.deltaTime);
        }
    }
}