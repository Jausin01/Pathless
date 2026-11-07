using Pathless.Player;
using UnityEngine;

namespace Pathless.Interaction
{
    /// <summary>
    /// Handles OPTIONAL interactions: when the interact button is pressed it checks a small circle
    /// just in front of the player and interacts with the closest object whose mode is Optional.
    /// It only looks when the button is pressed, so it costs nothing the rest of the time.
    /// Forced interactions are ignored here; they are handled by InteractionZone.
    /// Usage: put it on the Player (it needs PlayerInputReader, PlayerFacing and PlayerMotor on the same object).
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerFacing))]
    [RequireComponent(typeof(PlayerMotor))]
    public class InteractionSensor : MonoBehaviour
    {
        /// <summary>
        /// Distance (units) from the center of the player's body to the center of the check circle,
        /// measured in the facing direction. Larger values let the player interact from farther away.
        /// </summary>
        [SerializeField] private float reach = 0.6f;

        /// <summary>Radius (units) of the check circle. Larger values make interaction more forgiving.</summary>
        [SerializeField] private float radius = 0.5f;

        /// <summary>
        /// Offset from the player's feet (the transform position) to the middle of the body.
        /// The sprite pivot is at the feet, so the check is lifted to chest height.
        /// </summary>
        [SerializeField] private Vector2 bodyCenterOffset = new Vector2(0f, 0.35f);

        /// <summary>Raises the event when the interact button is pressed.</summary>
        private PlayerInputReader input;

        /// <summary>Tells which way the player is looking.</summary>
        private PlayerFacing facing;

        /// <summary>Lets us ignore interaction while the player is frozen (dialogue, cutscene).</summary>
        private PlayerMotor motor;

        /// <summary>Caches the components this script depends on.</summary>
        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            facing = GetComponent<PlayerFacing>();
            motor = GetComponent<PlayerMotor>();
        }

        /// <summary>Starts listening to the interact button.</summary>
        private void OnEnable()
        {
            input.InteractPressed += HandleInteractPressed;
        }

        /// <summary>Stops listening to the interact button.</summary>
        private void OnDisable()
        {
            input.InteractPressed -= HandleInteractPressed;
        }

        /// <summary>
        /// Looks for optional interactables inside the check circle and interacts with the closest one.
        /// </summary>
        private void HandleInteractPressed()
        {
            // While the player is frozen (for example during a dialogue) the button must not start new interactions.
            if (!motor.MovementEnabled)
            {
                return;
            }

            Vector2 center = GetSensorCenter(facing.Direction);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

            IInteractable closest = null;
            float closestDistanceSqr = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                // Trigger colliders are zones, not objects to talk to.
                if (hit.isTrigger)
                {
                    continue;
                }

                IInteractable candidate = hit.GetComponentInParent<IInteractable>();
                if (candidate == null || candidate.Mode != InteractionMode.Optional)
                {
                    continue;
                }

                // ClosestPoint measures to the nearest edge of the collider, so big objects are not penalized.
                float distanceSqr = (hit.ClosestPoint(center) - center).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closest = candidate;
                }
            }

            if (closest != null)
            {
                closest.Interact(gameObject);
            }
        }

        /// <summary>Calculates the world position of the check circle for a given facing direction.</summary>
        /// <param name="direction">Snapped facing direction (x and y are -1, 0 or 1).</param>
        /// <returns>The center of the check circle in world space.</returns>
        private Vector2 GetSensorCenter(Vector2 direction)
        {
            Vector2 origin = (Vector2)transform.position + bodyCenterOffset;
            return origin + direction.normalized * reach;
        }

        /// <summary>Draws the check circle in the Scene view when the Player is selected (editor helper).</summary>
        private void OnDrawGizmosSelected()
        {
            // Outside play mode the facing script has no direction yet, so look South like the player starts.
            Vector2 direction = Application.isPlaying && facing != null ? facing.Direction : Vector2.down;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetSensorCenter(direction), radius);
        }
    }
}
