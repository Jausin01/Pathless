using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pathless.Player
{
    /// <summary>
    /// Reads player input from the Input System asset and exposes it to the rest of the player scripts.
    /// This is the ONLY script that knows about input action names, so changing bindings or actions
    /// never requires touching movement, animation or interaction code.
    /// Usage: put it on the Player and assign the InputSystem_Actions asset in the inspector.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>
        /// The Input System asset that contains the action maps (InputSystem_Actions).
        /// Assign it in the inspector.
        /// </summary>
        [SerializeField] private InputActionAsset actions;

        /// <summary>Name of the action map that holds the gameplay actions (default: "Player").</summary>
        [SerializeField] private string actionMapName = "Player";

        /// <summary>Name of the Vector2 action used for walking (default: "Move").</summary>
        [SerializeField] private string moveActionName = "Move";

        /// <summary>Name of the button action used to interact (default: "Interact": E key, X / A on gamepad).</summary>
        [SerializeField] private string interactActionName = "Interact";

        /// <summary>Cached Move action, resolved once in Awake.</summary>
        private InputAction moveAction;

        /// <summary>Cached Interact action, resolved once in Awake.</summary>
        private InputAction interactAction;

        /// <summary>
        /// Current movement input. Each axis is between -1 and 1 (keyboard gives full values, a stick gives analog values).
        /// Read it every frame; it returns Vector2.zero if the action could not be found.
        /// </summary>
        public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>
        /// Raised once each time the interact button is pressed.
        /// Subscribe with InteractPressed += Handler and unsubscribe with -= in OnDisable.
        /// </summary>
        public event Action InteractPressed;

        /// <summary>Finds the actions by name. Runs before OnEnable so the actions are ready to be enabled.</summary>
        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerInputReader: no InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            InputActionMap map = actions.FindActionMap(actionMapName, true);
            moveAction = map.FindAction(moveActionName, true);
            interactAction = map.FindAction(interactActionName, true);
        }

        /// <summary>Starts listening to the actions when the component becomes active.</summary>
        private void OnEnable()
        {
            if (moveAction == null || interactAction == null)
            {
                return;
            }

            moveAction.Enable();
            interactAction.Enable();
            interactAction.performed += OnInteractPerformed;
        }

        /// <summary>Stops listening to the actions when the component is disabled or destroyed.</summary>
        private void OnDisable()
        {
            if (moveAction == null || interactAction == null)
            {
                return;
            }

            interactAction.performed -= OnInteractPerformed;
            moveAction.Disable();
            interactAction.Disable();
        }

        /// <summary>Called by the Input System when Interact is pressed; forwards it as a plain C# event.</summary>
        /// <param name="context">Input System callback data (unused, only the press itself matters).</param>
        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }
    }
}
