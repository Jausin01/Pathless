using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// List of every speaking character of the game, used to turn a name written in a .dialogue file
    /// (for example "Elder") into the character asset with its portraits and voice.
    /// Keep it up to date with Tools > Pathless > Dialogue > Refresh Character Library.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterLibrary", menuName = "Pathless/Dialogue/Character Library")]
    public class DialogueCharacterLibrary : ScriptableObject
    {
        /// <summary>All known characters.</summary>
        [SerializeField] private List<DialogueCharacter> characters = new List<DialogueCharacter>();

        /// <summary>Finds a character by the name shown on the name plate (not case sensitive).</summary>
        /// <param name="speakerName">Name written in the dialogue file.</param>
        /// <returns>The character, or null if there is none with that name.</returns>
        public DialogueCharacter Find(string speakerName)
        {
            foreach (DialogueCharacter character in characters)
            {
                if (character != null && string.Equals(character.DisplayName, speakerName, StringComparison.OrdinalIgnoreCase))
                {
                    return character;
                }
            }

            return null;
        }
    }
}
