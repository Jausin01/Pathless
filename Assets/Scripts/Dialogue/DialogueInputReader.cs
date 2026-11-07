using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Reads the buttons of the dialogue input map and raises events:
    /// "Advance" (Space, Enter, gamepad A) and "Up" / "Down" (arrows, W/S, d-pad, left stick; used to pick a choice).
    /// The input is only listened to while a dialogue is open, so it never conflicts with gameplay input.
    /// Usage: DialogueRunner calls SetListening(true) when a dialogue starts and SetListening(false) when it ends.
    /// </summary>
    public class DialogueInputReader : MonoBehaviour
    {
        /// <summary>Input Actions asset that contains the dialogue map (Assets/Settings/DialogueActions).</summary>
        [SerializeField] private InputActionAsset actions;

        /// <summary>Name of the action map inside the asset.</summary>
        [SerializeField] private string actionMapName = "Dialogue";

        /// <summary>Name of the "advance / skip / confirm" action inside the map.</summary>
        [SerializeField] private string advanceActionName = "Advance";

        /// <summary>Name of the "move selection up" action inside the map.</summary>
        [SerializeField] private string upActionName = "Up";

        /// <summary>Name of the "move selection down" action inside the map.</summary>
        [SerializeField] private string downActionName = "Down";

        /// <summary>The resolved advance action.</summary>
        private InputAction advanceAction;

        /// <summary>The resolved up action.</summary>
        private InputAction upAction;

        /// <summary>The resolved down action.</summary>
        private InputAction downAction;

        /// <summary>Raised once each time the player presses the advance button.</summary>
        public event Action AdvancePressed;

        /// <summary>Raised once each time the player presses up.</summary>
        public event Action UpPressed;

        /// <summary>Raised once each time the player presses down.</summary>
        public event Action DownPressed;

        /// <summary>Finds the actions. They stay disabled until SetListening(true) is called.</summary>
        private void Awake()
        {
            InputActionMap map = actions.FindActionMap(actionMapName, true);
            advanceAction = map.FindAction(advanceActionName, true);
            upAction = map.FindAction(upActionName, true);
            downAction = map.FindAction(downActionName, true);
        }

        /// <summary>Starts or stops listening to the buttons. Safe to call repeatedly.</summary>
        /// <param name="listening">True to listen, false to ignore the buttons.</param>
        public void SetListening(bool listening)
        {
            // Remove first so calling twice never subscribes twice (named methods, so they can be removed later).
            advanceAction.performed -= OnAdvancePerformed;
            upAction.performed -= OnUpPerformed;
            downAction.performed -= OnDownPerformed;

            if (listening)
            {
                advanceAction.performed += OnAdvancePerformed;
                upAction.performed += OnUpPerformed;
                downAction.performed += OnDownPerformed;
                advanceAction.Enable();
                upAction.Enable();
                downAction.Enable();
            }
            else
            {
                advanceAction.Disable();
                upAction.Disable();
                downAction.Disable();
            }
        }

        /// <summary>Stops listening when this object is disabled.</summary>
        private void OnDisable()
        {
            if (advanceAction != null)
            {
                SetListening(false);
            }
        }

        /// <summary>Forwards the advance press to the listeners.</summary>
        /// <param name="context">Input callback data (unused).</param>
        private void OnAdvancePerformed(InputAction.CallbackContext context)
        {
            AdvancePressed?.Invoke();
        }

        /// <summary>Forwards the up press to the listeners.</summary>
        /// <param name="context">Input callback data (unused).</param>
        private void OnUpPerformed(InputAction.CallbackContext context)
        {
            UpPressed?.Invoke();
        }

        /// <summary>Forwards the down press to the listeners.</summary>
        /// <param name="context">Input callback data (unused).</param>
        private void OnDownPerformed(InputAction.CallbackContext context)
        {
            DownPressed?.Invoke();
        }
    }
}
