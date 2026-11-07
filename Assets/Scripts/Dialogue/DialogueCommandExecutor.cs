using System.Collections;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Base class for whatever runs the "@command" lines of a .dialogue file (fade, move, camera...).
    /// DialogueRunner only knows this class, so the dialogue system does not depend on the cutscene system;
    /// the cutscene system (CutsceneDirector) plugs in by inheriting from it.
    /// </summary>
    public abstract class DialogueCommandExecutor : MonoBehaviour
    {
        /// <summary>
        /// Runs one command. The dialogue waits until this coroutine finishes before showing the next line.
        /// </summary>
        /// <param name="command">The command text without the "@", for example "fade out 0.5".</param>
        /// <returns>A coroutine that ends when the command is done.</returns>
        public abstract IEnumerator Execute(string command);
    }
}
