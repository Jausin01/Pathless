using Pathless.Core;
using Pathless.Dialogue;
using UnityEngine;

namespace Pathless.Npc
{
    /// <summary>
    /// Controls which way a standing NPC looks. It only writes the Animator parameters (MoveX, MoveY, IsMoving);
    /// the animation itself is played by the Animator (the idle blend tree of the character's controller, which has
    /// the same parameters as the player's). The NPC starts looking at "initialDirection" and, when a conversation
    /// starts with it (DialogueInteractable.Interacted), turns to look at whoever started it.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class NpcFacing : MonoBehaviour
    {
        /// <summary>Animator parameter hash for the horizontal facing.</summary>
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");

        /// <summary>Animator parameter hash for the vertical facing.</summary>
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        /// <summary>Animator parameter hash for the walking state.</summary>
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        /// <summary>The direction the NPC looks at when the scene starts (snapped to 8 directions). Down = towards the screen.</summary>
        [SerializeField] private Vector2 initialDirection = Vector2.down;

        /// <summary>True to turn towards the player when a conversation starts.</summary>
        [SerializeField] private bool turnToInteractor = true;

        /// <summary>The Animator that plays the idle animation.</summary>
        private Animator animator;

        /// <summary>The interactable of this NPC (may be missing, then the NPC never turns by itself).</summary>
        private DialogueInteractable interactable;

        /// <summary>Gets the components and applies the starting direction.</summary>
        private void Awake()
        {
            animator = GetComponent<Animator>();
            interactable = GetComponent<DialogueInteractable>();
            SetDirection(initialDirection);
        }

        /// <summary>Starts listening for conversations (named method, so it can be unsubscribed).</summary>
        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.Interacted += OnInteracted;
            }
        }

        /// <summary>Stops listening.</summary>
        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= OnInteracted;
            }
        }

        /// <summary>Turns the NPC to look at a point in the world.</summary>
        /// <param name="worldPoint">The point to look at.</param>
        public void FaceTowards(Vector2 worldPoint)
        {
            Vector2 toPoint = worldPoint - (Vector2)transform.position;
            if (toPoint.sqrMagnitude > 0.0001f)
            {
                SetDirection(toPoint);
            }
        }

        /// <summary>Called when a conversation starts: looks at whoever started it.</summary>
        /// <param name="interactor">The object that interacted (the player).</param>
        private void OnInteracted(GameObject interactor)
        {
            if (turnToInteractor && interactor != null)
            {
                FaceTowards(interactor.transform.position);
            }
        }

        /// <summary>Writes the direction (snapped to the 8 compass directions) to the Animator.</summary>
        /// <param name="direction">Any non-zero direction.</param>
        private void SetDirection(Vector2 direction)
        {
            Vector2 snapped = Direction8.Snap(direction);
            animator.SetFloat(MoveXHash, snapped.x);
            animator.SetFloat(MoveYHash, snapped.y);
            animator.SetBool(IsMovingHash, false);
        }
    }
}
