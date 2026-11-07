using UnityEngine;

namespace Pathless.Player
{
    /// <summary>
    /// Moves the player in 8 directions (free movement, Zelda style) using the Rigidbody2D,
    /// so walls and other solid colliders block the player correctly.
    /// It only handles movement; facing, animation and interaction live in separate scripts.
    /// Usage: put it on the Player next to the Rigidbody2D (gravity scale 0) and PlayerInputReader.
    /// Other systems (dialogue, cutscenes) can freeze the player with SetMovementEnabled(false).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMotor : MonoBehaviour
    {
        /// <summary>
        /// Walking speed in Unity units per second. With 32 pixels per unit, 2.8 is about 90 pixels per second.
        /// Tweak it in the inspector to change how fast the player walks.
        /// </summary>
        [SerializeField] private float moveSpeed = 2.8f;

        /// <summary>
        /// Minimum real speed (units per second) to count as "moving".
        /// Prevents the walk animation from playing while the player is pushing against a wall.
        /// </summary>
        [SerializeField] private float movingThreshold = 0.05f;

        /// <summary>The physics body that is moved.</summary>
        private Rigidbody2D body;

        /// <summary>Source of the raw movement input.</summary>
        private PlayerInputReader input;

        /// <summary>
        /// True when the player may move. Set it with SetMovementEnabled.
        /// While false, MoveInput is zero and the player stands still.
        /// </summary>
        public bool MovementEnabled { get; private set; } = true;

        /// <summary>
        /// The movement the player WANTS this frame (length 0 to 1), or zero while movement is disabled.
        /// Other scripts use it to know which way the player is trying to go, even if a wall blocks it.
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// True when the player is REALLY moving (real velocity above the threshold).
        /// The Animator driver uses it to choose between the idle and walk animations.
        /// </summary>
        public bool IsMoving { get; private set; }

        /// <summary>Movement forced by a cutscene (see SetScriptedInput). Only used while hasScriptedInput is true.</summary>
        private Vector2 scriptedInput;

        /// <summary>True while a cutscene is moving the player; this overrides the keyboard/gamepad and MovementEnabled.</summary>
        private bool hasScriptedInput;

        /// <summary>
        /// Makes the player walk as if this direction was pressed, ignoring the real input and MovementEnabled
        /// (dialogues freeze the player, but cutscenes must still be able to move it). Call every frame while walking.
        /// </summary>
        /// <param name="direction">Wanted movement (length 0 to 1).</param>
        public void SetScriptedInput(Vector2 direction)
        {
            scriptedInput = direction;
            hasScriptedInput = true;
        }

        /// <summary>Gives control back to the real input after a scripted walk.</summary>
        public void ClearScriptedInput()
        {
            hasScriptedInput = false;
            scriptedInput = Vector2.zero;
        }

        /// <summary>Caches the components this script depends on.</summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
        }

        /// <summary>Turns movement on or off, for example off while a dialogue box is open.</summary>
        /// <param name="value">True to allow movement, false to freeze the player.</param>
        public void SetMovementEnabled(bool value)
        {
            MovementEnabled = value;
        }

        /// <summary>Reads input and measures the real speed once per rendered frame.</summary>
        private void Update()
        {
            // ClampMagnitude keeps diagonals from being faster than straight lines while still allowing analog sticks.
            // A cutscene walk has priority over everything; otherwise normal input, unless movement is frozen.
            if (hasScriptedInput)
            {
                MoveInput = Vector2.ClampMagnitude(scriptedInput, 1f);
            }
            else
            {
                MoveInput = MovementEnabled ? Vector2.ClampMagnitude(input.Move, 1f) : Vector2.zero;
            }

            // Real velocity (after the physics step) tells us whether the player actually moved.
            IsMoving = body.linearVelocity.sqrMagnitude > movingThreshold * movingThreshold;
        }

        /// <summary>Applies the velocity in the physics step so collisions are resolved correctly.</summary>
        private void FixedUpdate()
        {
            body.linearVelocity = MoveInput * moveSpeed;
        }
    }
}
