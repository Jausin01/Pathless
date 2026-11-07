using UnityEngine;

namespace Pathless.CameraRig
{
    /// <summary>
    /// Makes the camera follow a target (the player). Pixel snapping is handled by the Pixel Perfect Camera
    /// component on the same camera, so this script only has to copy the target position.
    /// Usage: put it on the Main Camera and assign the player as the target.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        /// <summary>The transform to follow (normally the Player).</summary>
        [SerializeField] private Transform target;

        /// <summary>
        /// Offset added to the target position. The player's pivot is at the feet, so a positive Y
        /// (about 0.9 units) centers the camera on the body instead of the feet.
        /// </summary>
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.9f);

        /// <summary>Time in seconds of the smooth pan to a new target (0 = no pan is running).</summary>
        private float transitionDuration;

        /// <summary>Seconds passed since the pan began.</summary>
        private float transitionElapsed;

        /// <summary>Camera position (x, y) where the pan began.</summary>
        private Vector2 transitionStart;

        /// <summary>The transform being followed right now.</summary>
        public Transform Target => target;

        /// <summary>
        /// Changes what the camera follows. With a duration above zero the camera glides smoothly to the new target
        /// (used by cutscenes); with zero it jumps there.
        /// </summary>
        /// <param name="newTarget">The new transform to follow.</param>
        /// <param name="panSeconds">Pan time in seconds.</param>
        public void SetTarget(Transform newTarget, float panSeconds)
        {
            transitionStart = transform.position;
            transitionElapsed = 0f;
            transitionDuration = Mathf.Max(0f, panSeconds);
            target = newTarget;
        }

        /// <summary>Moves the camera after all other objects moved this frame, which avoids jitter.</summary>
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position;
            Vector2 goal = new Vector2(targetPosition.x + offset.x, targetPosition.y + offset.y);

            // While a pan is running, blend from where the camera was to the (possibly moving) goal.
            if (transitionElapsed < transitionDuration)
            {
                transitionElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionElapsed / transitionDuration));
                goal = Vector2.Lerp(transitionStart, goal, t);
            }

            // Keep the camera's own Z so it never ends up inside the 2D scene.
            transform.position = new Vector3(goal.x, goal.y, transform.position.z);
        }
    }
}
