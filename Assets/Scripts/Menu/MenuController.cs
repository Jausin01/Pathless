using System;
using Pathless.Dialogue;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// The menu logic: opens and closes it, moves the selection (wrapping around at the ends) and announces
    /// the chosen option. It does not draw anything (MenuView) and does not decide what an option does
    /// (MenuOptionHandler and MenuScreenRouter listen to OptionChosen).
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        /// <summary>The menu to show (title and options).</summary>
        [SerializeField] private MenuDefinition definition;

        /// <summary>The menu visuals.</summary>
        [SerializeField] private MenuView view;

        /// <summary>The menu buttons.</summary>
        [SerializeField] private MenuInputReader input;

        /// <summary>Optional: while this runner plays a dialogue the menu cannot be opened.</summary>
        [SerializeField] private DialogueRunner dialogueRunner;

        /// <summary>True while the menu is open.</summary>
        private bool isOpen;

        /// <summary>Index of the selected option.</summary>
        private int selectedIndex;

        /// <summary>True while the menu is open.</summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// While true the menu ignores its navigation buttons (a full screen is open and uses them instead).
        /// Set by MenuScreenRouter.
        /// </summary>
        public bool InputBlocked { get; set; }

        /// <summary>Raised when the menu opens.</summary>
        public event Action Opened;

        /// <summary>Raised when the menu closes.</summary>
        public event Action Closed;

        /// <summary>Raised when the player chooses an option. The argument is the option id.</summary>
        public event Action<string> OptionChosen;

        /// <summary>Builds the rows.</summary>
        private void Awake()
        {
            view.Build(definition);
        }

        /// <summary>Subscribes to the input events.</summary>
        private void OnEnable()
        {
            input.OpenPressed += OnOpenPressed;
            input.UpPressed += OnUpPressed;
            input.DownPressed += OnDownPressed;
            input.SubmitPressed += OnSubmitPressed;
            input.CancelPressed += OnCancelPressed;
        }

        /// <summary>Unsubscribes from the input events.</summary>
        private void OnDisable()
        {
            input.OpenPressed -= OnOpenPressed;
            input.UpPressed -= OnUpPressed;
            input.DownPressed -= OnDownPressed;
            input.SubmitPressed -= OnSubmitPressed;
            input.CancelPressed -= OnCancelPressed;
        }

        /// <summary>Opens the menu unless it is open or a dialogue is running.</summary>
        private void OnOpenPressed()
        {
            if (isOpen || (dialogueRunner != null && dialogueRunner.IsRunning))
            {
                return;
            }

            isOpen = true;
            selectedIndex = 0;
            view.SetSelected(selectedIndex);
            view.Open();
            input.SetMenuOpen(true);
            Opened?.Invoke();
        }

        /// <summary>Closes the menu (ignored while a screen is open).</summary>
        private void OnCancelPressed()
        {
            if (!isOpen || InputBlocked)
            {
                return;
            }

            isOpen = false;
            view.Close();
            input.SetMenuOpen(false);
            Closed?.Invoke();
        }

        /// <summary>Moves the selection up.</summary>
        private void OnUpPressed()
        {
            MoveSelection(-1);
        }

        /// <summary>Moves the selection down.</summary>
        private void OnDownPressed()
        {
            MoveSelection(1);
        }

        /// <summary>Moves the selection, wrapping from the last option to the first and back.</summary>
        /// <param name="direction">-1 for up, 1 for down.</param>
        private void MoveSelection(int direction)
        {
            int count = definition.Entries.Count;
            if (count == 0 || InputBlocked)
            {
                return;
            }

            selectedIndex = (selectedIndex + direction + count) % count;
            view.SetSelected(selectedIndex);
        }

        /// <summary>Announces the selected option.</summary>
        private void OnSubmitPressed()
        {
            if (definition.Entries.Count == 0 || InputBlocked)
            {
                return;
            }

            OptionChosen?.Invoke(definition.Entries[selectedIndex].Id);
        }
    }
}
