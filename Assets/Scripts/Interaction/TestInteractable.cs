using UnityEngine;

namespace Pathless.Interaction
{
    /// <summary>
    /// Placeholder interactable used by the test scene blocks. It only writes a message to the Console,
    /// which proves that the interaction system works. Replace it with the real dialogue trigger later.
    /// Usage: put it on a block with a solid collider and choose the Mode in the inspector.
    /// </summary>
    public class TestInteractable : MonoBehaviour, IInteractable
    {
        /// <summary>Optional = needs the interact button. Forced = triggered by an InteractionZone.</summary>
        [SerializeField] private InteractionMode mode = InteractionMode.Optional;

        /// <summary>Name written in the Console message so you can tell the blocks apart.</summary>
        [SerializeField] private string displayName = "Test Block";

        /// <summary>How this object is triggered (read by the sensor and the zone).</summary>
        public InteractionMode Mode => mode;

        /// <summary>Writes a Console message when the interaction starts.</summary>
        /// <param name="interactor">The GameObject that triggered the interaction (the player).</param>
        public void Interact(GameObject interactor)
        {
            Debug.Log($"[{displayName}] interaction started by {interactor.name} (mode: {mode})", this);
        }
    }
}
