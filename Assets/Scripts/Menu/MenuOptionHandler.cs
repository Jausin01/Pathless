using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// Decides what each menu option does. For now only "quit" works (it stops Play mode in the editor and
    /// closes the game in a build); every other option just logs a message until its screen exists.
    /// Add new cases here (or new handler scripts) as the game gets inventory, save, settings, etc.
    /// </summary>
    public class MenuOptionHandler : MonoBehaviour
    {
        /// <summary>The menu to listen to.</summary>
        [SerializeField] private MenuController controller;

        /// <summary>Optional: options that open a full screen (Status, Quests...) are handled by this router, so they are not reported as missing.</summary>
        [SerializeField] private MenuScreenRouter screenRouter;

        /// <summary>Subscribes to the chosen-option event.</summary>
        private void OnEnable()
        {
            controller.OptionChosen += OnOptionChosen;
        }

        /// <summary>Unsubscribes from the chosen-option event.</summary>
        private void OnDisable()
        {
            controller.OptionChosen -= OnOptionChosen;
        }

        /// <summary>Runs the action of the chosen option.</summary>
        /// <param name="optionId">Id of the chosen option.</param>
        private void OnOptionChosen(string optionId)
        {
            switch (optionId)
            {
                case "quit":
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;

                default:
                    if (screenRouter != null && screenRouter.Handles(optionId))
                    {
                        break;
                    }

                    Debug.Log($"MenuOptionHandler: option '{optionId}' is not implemented yet.");
                    break;
            }
        }
    }
}
