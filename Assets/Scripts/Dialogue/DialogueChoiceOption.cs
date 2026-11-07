using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// One option of a choice (a "- text => @set flag" line in a .dialogue file): the text the player sees, the flag
    /// commands that run when it is chosen, and optionally another conversation to jump to.
    /// </summary>
    [Serializable]
    public class DialogueChoiceOption
    {
        /// <summary>The text shown in the choice list.</summary>
        [SerializeField] private string label;

        /// <summary>Flag commands run when chosen, for example "set helped_elder" or "add elder_trust 1".</summary>
        [SerializeField] private List<string> actions = new List<string>();

        /// <summary>Id of the conversation to jump to (empty = continue with the line after the choice).</summary>
        [SerializeField] private string gotoId;

        /// <summary>The conversation found for gotoId (assigned by the importer).</summary>
        [SerializeField] private DialogueSequence gotoSequence;

        /// <summary>Creates an empty option (used by the Unity serializer).</summary>
        public DialogueChoiceOption()
        {
        }

        /// <summary>Creates an option. Used by the .dialogue importer.</summary>
        /// <param name="label">Text shown to the player.</param>
        /// <param name="actions">Flag commands to run when chosen.</param>
        /// <param name="gotoId">Conversation id to jump to, or empty.</param>
        public DialogueChoiceOption(string label, List<string> actions, string gotoId)
        {
            this.label = label;
            this.actions = actions;
            this.gotoId = gotoId;
        }

        /// <summary>The text shown in the choice list.</summary>
        public string Label => label;

        /// <summary>Flag commands run when chosen.</summary>
        public IReadOnlyList<string> Actions => actions;

        /// <summary>Id of the conversation to jump to (empty if none).</summary>
        public string GotoId => gotoId;

        /// <summary>The conversation to jump to (null if none).</summary>
        public DialogueSequence GotoSequence => gotoSequence;

        /// <summary>Finds the conversation named by GotoId. Called by the importer once all conversations exist.</summary>
        /// <param name="find">Returns the conversation with an id, or null.</param>
        public void ResolveGoto(Func<string, DialogueSequence> find)
        {
            gotoSequence = string.IsNullOrEmpty(gotoId) ? null : find(gotoId);
        }
    }
}
