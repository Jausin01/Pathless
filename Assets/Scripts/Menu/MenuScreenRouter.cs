using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// Connects the menu to its full screens. When the player chooses an option whose id matches a MenuScreen,
    /// the router opens that screen, blocks the menu's own buttons and forwards the buttons to the screen;
    /// Cancel closes the screen and gives control back to the menu.
    /// </summary>
    public class MenuScreenRouter : MonoBehaviour
    {
        /// <summary>The menu that announces the chosen option.</summary>
        [SerializeField] private MenuController controller;

        /// <summary>The menu buttons.</summary>
        [SerializeField] private MenuInputReader input;

        /// <summary>The screens that can be opened.</summary>
        [SerializeField] private List<MenuScreen> screens = new List<MenuScreen>();

        /// <summary>The screen that is open, or null.</summary>
        private MenuScreen active;

        /// <summary>Frame in which the active screen was opened (the button press that opened it must not reach it).</summary>
        private int openedFrame;

        /// <summary>Subscribes to the menu events.</summary>
        private void OnEnable()
        {
            controller.OptionChosen += OnOptionChosen;
            input.UpPressed += OnUpPressed;
            input.DownPressed += OnDownPressed;
            input.LeftPressed += OnLeftPressed;
            input.RightPressed += OnRightPressed;
            input.SubmitPressed += OnSubmitPressed;
            input.CancelPressed += OnCancelPressed;
        }

        /// <summary>Unsubscribes from the menu events.</summary>
        private void OnDisable()
        {
            controller.OptionChosen -= OnOptionChosen;
            input.UpPressed -= OnUpPressed;
            input.DownPressed -= OnDownPressed;
            input.LeftPressed -= OnLeftPressed;
            input.RightPressed -= OnRightPressed;
            input.SubmitPressed -= OnSubmitPressed;
            input.CancelPressed -= OnCancelPressed;
        }

        /// <summary>Tells if an option id opens one of the screens.</summary>
        /// <param name="optionId">The menu option id.</param>
        /// <returns>True if a screen handles it.</returns>
        public bool Handles(string optionId)
        {
            return FindScreen(optionId) != null;
        }

        /// <summary>Opens the screen of the chosen option, if there is one.</summary>
        /// <param name="optionId">Id of the chosen option.</param>
        private void OnOptionChosen(string optionId)
        {
            MenuScreen screen = FindScreen(optionId);
            if (active != null || screen == null)
            {
                return;
            }

            active = screen;
            openedFrame = Time.frameCount;
            controller.InputBlocked = true;
            screen.Open();
        }

        /// <summary>Finds the screen with an id.</summary>
        private MenuScreen FindScreen(string optionId)
        {
            foreach (MenuScreen screen in screens)
            {
                if (screen != null && screen.ScreenId == optionId)
                {
                    return screen;
                }
            }

            return null;
        }

        /// <summary>True when the active screen may receive a button (not in the frame it was opened).</summary>
        private bool CanForward()
        {
            return active != null && Time.frameCount != openedFrame;
        }

        /// <summary>Forwards up.</summary>
        private void OnUpPressed()
        {
            if (CanForward()) active.OnUp();
        }

        /// <summary>Forwards down.</summary>
        private void OnDownPressed()
        {
            if (CanForward()) active.OnDown();
        }

        /// <summary>Forwards left.</summary>
        private void OnLeftPressed()
        {
            if (CanForward()) active.OnLeft();
        }

        /// <summary>Forwards right.</summary>
        private void OnRightPressed()
        {
            if (CanForward()) active.OnRight();
        }

        /// <summary>Forwards submit.</summary>
        private void OnSubmitPressed()
        {
            if (CanForward()) active.OnSubmit();
        }

        /// <summary>Closes the active screen. The menu is unblocked one frame later so the same press does not also close the menu.</summary>
        private void OnCancelPressed()
        {
            if (!CanForward())
            {
                return;
            }

            active.Close();
            active = null;
            StartCoroutine(UnblockNextFrame());
        }

        /// <summary>Gives the buttons back to the menu after one frame.</summary>
        private IEnumerator UnblockNextFrame()
        {
            yield return null;
            controller.InputBlocked = false;
        }
    }
}
