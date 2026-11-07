using System.Collections;
using Pathless.Player;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Freezes the player while a dialogue is running and releases the player afterwards.
    /// The release waits one frame so the same button press that closed the dialogue
    /// cannot immediately trigger another interaction.
    /// </summary>
    public class DialoguePlayerLock : MonoBehaviour
    {
        /// <summary>The dialogue runner to listen to.</summary>
        [SerializeField] private DialogueRunner runner;

        /// <summary>The player motor to freeze. Assigned by the scene (the player is spawned in the scene).</summary>
        [SerializeField] private PlayerMotor playerMotor;

        /// <summary>Subscribes to the runner events.</summary>
        private void OnEnable()
        {
            runner.DialogueStarted += OnDialogueStarted;
            runner.DialogueEnded += OnDialogueEnded;
        }

        /// <summary>Unsubscribes from the runner events.</summary>
        private void OnDisable()
        {
            runner.DialogueStarted -= OnDialogueStarted;
            runner.DialogueEnded -= OnDialogueEnded;
        }

        /// <summary>Freezes the player.</summary>
        private void OnDialogueStarted()
        {
            if (playerMotor != null)
            {
                playerMotor.SetMovementEnabled(false);
            }
        }

        /// <summary>Starts the delayed release.</summary>
        private void OnDialogueEnded()
        {
            StartCoroutine(ReleaseNextFrame());
        }

        /// <summary>Waits one frame, then lets the player move again.</summary>
        private IEnumerator ReleaseNextFrame()
        {
            yield return null;

            if (playerMotor != null)
            {
                playerMotor.SetMovementEnabled(true);
            }
        }
    }
}
