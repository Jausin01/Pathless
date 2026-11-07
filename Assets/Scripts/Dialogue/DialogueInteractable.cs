using System;
using Pathless.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Makes an object start a dialogue when interacted with.
    /// Put it on an NPC (Optional mode: press E / A) or together with an InteractionZone (Forced mode: starts on touch).
    /// </summary>
    public class DialogueInteractable : MonoBehaviour, IInteractable
    {
        /// <summary>Optional = the player must press the interact button; Forced = starts automatically.</summary>
        [SerializeField] private InteractionMode mode = InteractionMode.Optional;

        /// <summary>
        /// Forced mode only: play the conversation just once per game, even if the scene is left and entered again
        /// (remembered by DialogueProgress). Turn it off for a forced conversation that must repeat.
        /// </summary>
        [SerializeField] private bool playOnce = true;

        /// <summary>The conversation to play.</summary>
        [SerializeField] private DialogueSequence sequence;

        /// <summary>The runner that plays the conversation (the DialogueSystem in the scene).</summary>
        [SerializeField] private DialogueRunner runner;

        /// <summary>Warns in the Console when the conversation is tagged for a different mode than this object uses.</summary>
        private void OnValidate()
        {
            if (sequence != null && sequence.Mode != mode)
            {
                Debug.LogWarning($"DialogueInteractable on '{name}': mode is {mode} but '{sequence.name}' is tagged {sequence.Mode}.", this);
            }
        }

        /// <summary>How this object is triggered.</summary>
        /// <summary>
        /// Raised when a conversation really starts because of an interaction. The argument is the object that
        /// interacted (the player). Other components, like NpcFacing, use it to react (for example turning to look at the player).
        /// </summary>
        public event Action<GameObject> Interacted;

        public InteractionMode Mode => mode;

        /// <summary>Starts the dialogue.</summary>
        /// <param name="interactor">The object that interacted (the player).</param>
        public void Interact(GameObject interactor)
        {
            bool remember = mode == InteractionMode.Forced && playOnce && sequence != null;
            string key = remember ? DialogueProgress.MakeKey(SceneManager.GetActiveScene().name, sequence.name) : null;

            if (remember && DialogueProgress.HasPlayed(key))
            {
                return;
            }

            // Only count it as played if it really starts (StartDialogue ignores the call while another dialogue runs).
            bool canStart = !runner.IsRunning;
            runner.StartDialogue(sequence);
            if (canStart)
            {
                Interacted?.Invoke(interactor);
            }

            if (remember && canStart)
            {
                DialogueProgress.MarkPlayed(key);
            }
        }
    }
}
