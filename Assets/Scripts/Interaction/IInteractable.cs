using UnityEngine;

namespace Pathless.Interaction
{
    /// <summary>
    /// Contract for anything the player can interact with (NPCs, signs, chests...).
    /// Implement it on a MonoBehaviour placed on the object (or on a parent of its collider).
    /// The InteractionSensor (optional interactions) and the InteractionZone (forced interactions)
    /// find it with GetComponentInParent and call Interact.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Whether this object is triggered by the interact button or by entering a zone.</summary>
        InteractionMode Mode { get; }

        /// <summary>
        /// Called when the interaction starts. Put the real behaviour here (open a dialogue, open a chest...).
        /// </summary>
        /// <param name="interactor">The GameObject that triggered it (the player).</param>
        void Interact(GameObject interactor);
    }
}
