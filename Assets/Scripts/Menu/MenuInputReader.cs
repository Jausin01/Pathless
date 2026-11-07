using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pathless.Menu
{
    /// <summary>
    /// Reads the menu buttons from the "Menu" input map and raises one event for each.
    /// While the menu is closed only the Open button is active; while it is open only Up, Down, Left, Right, Submit
    /// and Cancel are active. This way Escape can open AND close the menu without both actions firing together.
    /// Subscriptions use named methods added in OnEnable and removed in OnDisable: the Input Actions asset outlives
    /// Play sessions, so a callback that is never removed would later hit destroyed objects.
    /// </summary>
    public class MenuInputReader : MonoBehaviour
    {
        /// <summary>Input Actions asset that contains the menu map (Assets/Settings/MenuActions).</summary>
        [SerializeField] private InputActionAsset actions;

        /// <summary>Name of the action map inside the asset.</summary>
        [SerializeField] private string actionMapName = "Menu";

        /// <summary>The Open action (Escape / Tab / Start): opens the menu.</summary>
        private InputAction openAction;

        /// <summary>The Up action: moves the selection up.</summary>
        private InputAction upAction;

        /// <summary>The Down action: moves the selection down.</summary>
        private InputAction downAction;

        /// <summary>The Left action: used by screens with tabs.</summary>
        private InputAction leftAction;

        /// <summary>The Right action: used by screens with tabs.</summary>
        private InputAction rightAction;

        /// <summary>The Submit action (Enter / Space / A): chooses the selected option.</summary>
        private InputAction submitAction;

        /// <summary>The Cancel action (Escape / B): closes the menu or the open screen.</summary>
        private InputAction cancelAction;

        /// <summary>Raised when the player presses the open button (menu closed).</summary>
        public event Action OpenPressed;

        /// <summary>Raised when the player presses up (menu open).</summary>
        public event Action UpPressed;

        /// <summary>Raised when the player presses down (menu open).</summary>
        public event Action DownPressed;

        /// <summary>Raised when the player presses left (menu open).</summary>
        public event Action LeftPressed;

        /// <summary>Raised when the player presses right (menu open).</summary>
        public event Action RightPressed;

        /// <summary>Raised when the player presses submit (menu open).</summary>
        public event Action SubmitPressed;

        /// <summary>Raised when the player presses cancel (menu open).</summary>
        public event Action CancelPressed;

        /// <summary>Finds the actions.</summary>
        private void Awake()
        {
            InputActionMap map = actions.FindActionMap(actionMapName, true);
            openAction = map.FindAction("Open", true);
            upAction = map.FindAction("Up", true);
            downAction = map.FindAction("Down", true);
            leftAction = map.FindAction("Left", true);
            rightAction = map.FindAction("Right", true);
            submitAction = map.FindAction("Submit", true);
            cancelAction = map.FindAction("Cancel", true);
        }

        /// <summary>Subscribes to the actions and starts in the closed state (only Open is listened to).</summary>
        private void OnEnable()
        {
            openAction.performed += OnOpenPerformed;
            upAction.performed += OnUpPerformed;
            downAction.performed += OnDownPerformed;
            leftAction.performed += OnLeftPerformed;
            rightAction.performed += OnRightPerformed;
            submitAction.performed += OnSubmitPerformed;
            cancelAction.performed += OnCancelPerformed;
            SetMenuOpen(false);
        }

        /// <summary>Unsubscribes and stops all listening.</summary>
        private void OnDisable()
        {
            openAction.performed -= OnOpenPerformed;
            upAction.performed -= OnUpPerformed;
            downAction.performed -= OnDownPerformed;
            leftAction.performed -= OnLeftPerformed;
            rightAction.performed -= OnRightPerformed;
            submitAction.performed -= OnSubmitPerformed;
            cancelAction.performed -= OnCancelPerformed;

            openAction.Disable();
            SetNavigationEnabled(false);
        }

        /// <summary>Switches between the "closed" buttons (Open) and the "open" buttons (navigation).</summary>
        /// <param name="isOpen">True when the menu is open.</param>
        public void SetMenuOpen(bool isOpen)
        {
            if (isOpen)
            {
                openAction.Disable();
            }
            else
            {
                openAction.Enable();
            }

            SetNavigationEnabled(isOpen);
        }

        /// <summary>Enables or disables Up, Down, Left, Right, Submit and Cancel together.</summary>
        /// <param name="enabled">True to enable them.</param>
        private void SetNavigationEnabled(bool enabled)
        {
            foreach (InputAction action in new[] { upAction, downAction, leftAction, rightAction, submitAction, cancelAction })
            {
                if (enabled)
                {
                    action.Enable();
                }
                else
                {
                    action.Disable();
                }
            }
        }

        /// <summary>Forwards the open press.</summary>
        private void OnOpenPerformed(InputAction.CallbackContext context)
        {
            OpenPressed?.Invoke();
        }

        /// <summary>Forwards the up press.</summary>
        private void OnUpPerformed(InputAction.CallbackContext context)
        {
            UpPressed?.Invoke();
        }

        /// <summary>Forwards the down press.</summary>
        private void OnDownPerformed(InputAction.CallbackContext context)
        {
            DownPressed?.Invoke();
        }

        /// <summary>Forwards the left press.</summary>
        private void OnLeftPerformed(InputAction.CallbackContext context)
        {
            LeftPressed?.Invoke();
        }

        /// <summary>Forwards the right press.</summary>
        private void OnRightPerformed(InputAction.CallbackContext context)
        {
            RightPressed?.Invoke();
        }

        /// <summary>Forwards the submit press.</summary>
        private void OnSubmitPerformed(InputAction.CallbackContext context)
        {
            SubmitPressed?.Invoke();
        }

        /// <summary>Forwards the cancel press.</summary>
        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            CancelPressed?.Invoke();
        }
    }
}
