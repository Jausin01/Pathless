using UnityEngine;

namespace Pathless.Player
{
    /// <summary>
    /// Feeds the player's Animator with the parameters it needs. This script does NOT play or choose animations:
    /// the Animator Controller (two blend trees, Idle and Walk) decides which clip is shown.
    /// Parameters sent: MoveX / MoveY (facing direction) and IsMoving (walk or idle).
    /// Usage: put it on the Player next to the Animator that uses Player.controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerFacing))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        /// <summary>Cached hash of the "MoveX" Animator parameter (faster than using the string every frame).</summary>
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");

        /// <summary>Cached hash of the "MoveY" Animator parameter.</summary>
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        /// <summary>Cached hash of the "IsMoving" Animator parameter.</summary>
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        /// <summary>The Animator that receives the parameters.</summary>
        private Animator animator;

        /// <summary>Tells whether the player is really moving.</summary>
        private PlayerMotor motor;

        /// <summary>Tells which direction the player is facing.</summary>
        private PlayerFacing facing;

        /// <summary>Caches the components this script depends on.</summary>
        private void Awake()
        {
            animator = GetComponent<Animator>();
            motor = GetComponent<PlayerMotor>();
            facing = GetComponent<PlayerFacing>();
        }

        /// <summary>
        /// Sends the parameters after all Update calls, so facing and motor already hold this frame's values.
        /// </summary>
        private void LateUpdate()
        {
            animator.SetFloat(MoveXHash, facing.Direction.x);
            animator.SetFloat(MoveYHash, facing.Direction.y);
            animator.SetBool(IsMovingHash, motor.IsMoving);
        }
    }
}
