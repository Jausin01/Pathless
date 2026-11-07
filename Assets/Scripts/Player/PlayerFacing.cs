using Pathless.Core;
using UnityEngine;

namespace Pathless.Player
{
    /// <summary>
    /// Remembers which of the 8 compass directions the player is facing.
    /// The facing follows the movement INPUT (not the real velocity), so the player still turns
    /// towards a wall or an NPC when pushing against it. When there is no input the last direction is kept.
    /// Other scripts read Direction: the Animator driver (which sprites to show) and the interaction sensor (where to look).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerFacing : MonoBehaviour
    {
        /// <summary>
        /// Direction the player faces when the scene starts. Vector2.down = facing the camera (South).
        /// </summary>
        [SerializeField] private Vector2 initialDirection = Vector2.down;

        /// <summary>Source of the movement input that decides the facing.</summary>
        private PlayerMotor motor;

        /// <summary>
        /// Current facing, snapped to 8 directions (x and y are -1, 0 or 1; diagonals are NOT normalized).
        /// Use Direction.normalized if you need a unit-length vector.
        /// </summary>
        public Vector2 Direction { get; private set; }

        /// <summary>Caches the motor and sets the starting direction.</summary>
        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            Direction = Direction8.Snap(initialDirection);
        }

        /// <summary>Turns the player to a direction without moving (used by cutscenes, "@face Player north").</summary>
        /// <param name="direction">Any direction vector; it is snapped to the 8 compass directions.</param>
        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0f)
            {
                Direction = Direction8.Snap(direction);
            }
        }

        /// <summary>Updates the facing whenever the player is pressing a direction.</summary>
        private void Update()
        {
            // 0.01 is a squared length (input length 0.1): small enough to ignore stick noise.
            if (motor.MoveInput.sqrMagnitude > 0.01f)
            {
                Direction = Direction8.Snap(motor.MoveInput);
            }
        }
    }
}
