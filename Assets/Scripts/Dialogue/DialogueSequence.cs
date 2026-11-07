using System.Collections.Generic;
using Pathless.Interaction;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// A complete conversation: an ordered list of dialogue lines.
    /// Create one with Assets > Create > Pathless > Dialogue > Sequence and assign it to a DialogueInteractable.
    /// Keeping each conversation in its own asset makes it easy to write and reuse scenes of the story.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Pathless/Dialogue/Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        /// <summary>The lines in the order they are shown.</summary>
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        /// <summary>
        /// How this conversation is meant to be started (set by the "forced" / "optional" tag in .dialogue files).
        /// DialogueInteractable warns in the Inspector if its own mode differs.
        /// </summary>
        [SerializeField] private InteractionMode mode = InteractionMode.Optional;

        /// <summary>How this conversation is meant to be started.</summary>
        public InteractionMode Mode => mode;

        /// <summary>Fills the sequence with imported data. Called by the .dialogue importer only.</summary>
        /// <param name="newMode">Forced or Optional.</param>
        /// <param name="newLines">The lines in order.</param>
        public void Setup(InteractionMode newMode, List<DialogueLine> newLines)
        {
            mode = newMode;
            lines = newLines;
        }

        /// <summary>The lines in the order they are shown (read only).</summary>
        public IReadOnlyList<DialogueLine> Lines => lines;
    }
}
