using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// The asset created from one .dialogue file (one scene or area of the game).
    /// It only lists the conversations found in the file; each conversation is also a DialogueSequence
    /// sub-asset that you drag into a DialogueInteractable. Created by the importer, never by hand.
    /// </summary>
    public class DialogueScript : ScriptableObject
    {
        /// <summary>Area name from the "# Area" header of the file (can be empty).</summary>
        [SerializeField] private string areaName;

        /// <summary>The conversations of the file, in file order.</summary>
        [SerializeField] private List<DialogueSequence> sequences = new List<DialogueSequence>();

        /// <summary>Area name from the file header.</summary>
        public string AreaName => areaName;

        /// <summary>The conversations of the file.</summary>
        public IReadOnlyList<DialogueSequence> Sequences => sequences;

        /// <summary>Fills the asset with imported data. Called by the importer only.</summary>
        /// <param name="newAreaName">Area name.</param>
        /// <param name="newSequences">Conversations in file order.</param>
        public void Setup(string newAreaName, List<DialogueSequence> newSequences)
        {
            areaName = newAreaName;
            sequences = newSequences;
        }
    }
}
