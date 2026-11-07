using System.Collections;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// A group of "@commands" that the CutsceneDirector does not know by itself (party, music, sound...). Each module
    /// is its own component on the cutscene system object, lists the verbs it handles, and runs them. The director
    /// asks every module in its "modules" list when it meets a command it does not handle itself, so new kinds of
    /// commands can be added without touching the director.
    /// </summary>
    public abstract class CommandModule : MonoBehaviour
    {
        /// <summary>Tells whether this module runs the given command word (for example "party").</summary>
        /// <param name="verb">The first word of the command, in lower case.</param>
        /// <returns>True if this module handles it.</returns>
        public abstract bool Handles(string verb);

        /// <summary>Runs the command. The dialogue waits until the coroutine ends.</summary>
        /// <param name="words">The command split into words; words[0] is the verb.</param>
        /// <param name="command">The whole command text (used in warning messages).</param>
        public abstract IEnumerator Run(string[] words, string command);

        /// <summary>Writes a warning about a command that could not run.</summary>
        /// <param name="command">The whole command text.</param>
        /// <param name="problem">What is wrong and how to fix it.</param>
        protected void Warn(string command, string problem)
        {
            Debug.LogWarning($"{GetType().Name}: '@{command}' skipped, {problem}", this);
        }
    }
}
