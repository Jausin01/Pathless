using System.Collections;
using Pathless.CameraRig;
using UnityEngine;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Moves the game camera during cutscenes by changing what the CameraFollow follows (an actor or a point),
    /// with a smooth pan. Remembers the normal target (the player) so it can be restored when the cutscene ends.
    /// </summary>
    public class CutsceneCamera : MonoBehaviour
    {
        /// <summary>The camera follow script on the Main Camera. Assigned by the scene builder.</summary>
        [SerializeField] private CameraFollow follow;

        /// <summary>The target the camera follows in normal gameplay.</summary>
        private Transform defaultTarget;

        /// <summary>Invisible object used as a target when the camera focuses on a point.</summary>
        private Transform focusPoint;

        /// <summary>Time in seconds of the pan back to the normal target when a cutscene ends.</summary>
        private const float ResetPanSeconds = 0.5f;

        /// <summary>Remembers the normal target.</summary>
        private void Awake()
        {
            defaultTarget = follow.Target;
        }

        /// <summary>Pans the camera to an object and waits until the pan is done.</summary>
        /// <param name="target">The object to follow from now on.</param>
        /// <param name="panSeconds">Pan time in seconds.</param>
        /// <returns>A coroutine that ends when the pan is done.</returns>
        public IEnumerator FocusOn(Transform target, float panSeconds)
        {
            follow.SetTarget(target, panSeconds);
            yield return new WaitForSeconds(panSeconds);
        }

        /// <summary>Pans the camera to a world position and waits until the pan is done.</summary>
        /// <param name="point">World position to look at.</param>
        /// <param name="panSeconds">Pan time in seconds.</param>
        /// <returns>A coroutine that ends when the pan is done.</returns>
        public IEnumerator FocusOnPoint(Vector2 point, float panSeconds)
        {
            if (focusPoint == null)
            {
                focusPoint = new GameObject("CameraFocusPoint").transform;
            }

            focusPoint.position = new Vector3(point.x, point.y, 0f);
            return FocusOn(focusPoint, panSeconds);
        }

        /// <summary>Returns the camera to its normal target (called when a dialogue or cutscene ends).</summary>
        public void ResetToDefault()
        {
            if (follow.Target != defaultTarget)
            {
                follow.SetTarget(defaultTarget, ResetPanSeconds);
            }
        }
    }
}
