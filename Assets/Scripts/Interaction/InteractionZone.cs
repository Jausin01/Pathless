using Pathless.Player;
using UnityEngine;

namespace Pathless.Interaction
{
    /// <summary>
    /// Handles FORCED interactions: when the player walks into this trigger area, the IInteractable found
    /// on this object or on one of its parents is triggered automatically (no button press).
    /// Usage: create a child object under the interactable, add a Collider2D set as trigger plus this script,
    /// and size the collider to the area that should start the event.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractionZone : MonoBehaviour
    {
        /// <summary>
        /// If true the interaction happens only the first time the player enters.
        /// If false it happens again every time the player leaves and re-enters the zone.
        /// </summary>
        [SerializeField] private bool triggerOnce = true;

        /// <summary>The object that is triggered, found on this object or one of its parents.</summary>
        private IInteractable interactable;

        /// <summary>Remembers that the zone already fired (used when triggerOnce is true).</summary>
        private bool hasTriggered;

        /// <summary>Finds the interactable that this zone controls.</summary>
        private void Awake()
        {
            interactable = GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                Debug.LogWarning("InteractionZone: no IInteractable found on this object or its parents.", this);
            }
        }

        /// <summary>Editor helper: when the component is first added, make its collider a trigger.</summary>
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>Called by Unity when another collider enters this trigger area.</summary>
        /// <param name="other">The collider that entered; only the player's body counts.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (interactable == null || (triggerOnce && hasTriggered))
            {
                return;
            }

            // Only the player can start forced interactions (checked by the presence of PlayerMotor).
            PlayerMotor player = other.GetComponentInParent<PlayerMotor>();
            if (player == null)
            {
                return;
            }

            hasTriggered = true;
            interactable.Interact(player.gameObject);
        }
    }
}
