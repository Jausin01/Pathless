using Pathless.Interaction;
using Pathless.Player;
using UnityEngine;

namespace Pathless.SceneFlow
{
    /// <summary>
    /// A door or exit: when the player walks into its trigger, the game changes scene.
    /// Usage: an empty object with a trigger Collider2D, an InteractionZone (it detects the player) and this script.
    /// It freezes the player while the screen fades out.
    /// </summary>
    public class SceneTransitionZone : MonoBehaviour, IInteractable
    {
        /// <summary>Scene to load (file name without ".unity"; it must be in the Build Settings).</summary>
        [SerializeField] private string targetScene;

        /// <summary>Spawn id inside the target scene where the player appears.</summary>
        [SerializeField] private string targetSpawnId = "default";

        /// <summary>Optional conversation id (from the target scene's arrival script) to play after arriving.</summary>
        [SerializeField] private string continueSequenceId;

        /// <summary>The loader that performs the change.</summary>
        [SerializeField] private SceneLoader loader;

        /// <summary>Always forced: it starts by itself when the player touches the zone.</summary>
        public InteractionMode Mode => InteractionMode.Forced;

        /// <summary>Freezes the player and starts the scene change.</summary>
        /// <param name="interactor">The player object.</param>
        public void Interact(GameObject interactor)
        {
            PlayerMotor motor = interactor.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                motor.ClearScriptedInput();
                motor.SetMovementEnabled(false);
            }

            loader.StartCoroutine(loader.GoTo(targetScene, targetSpawnId, continueSequenceId));
        }
    }
}
