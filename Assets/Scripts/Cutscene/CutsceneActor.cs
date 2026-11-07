using System;
using System.Collections;
using System.Collections.Generic;
using Pathless.Npc;
using Pathless.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Makes an object controllable by cutscene commands under a name ("Player", "Elder"...).
    /// Put it on the player and on every NPC that must walk or turn in a cutscene, then use that name in
    /// "@move Elder 3.5 0" or "@face Elder north".
    /// A player-style object (it has a PlayerMotor) walks with its real movement and walk animation;
    /// anything else (a block, a sign) simply slides to the target.
    /// </summary>
    public class CutsceneActor : MonoBehaviour
    {
        /// <summary>Name used in cutscene commands (not case sensitive, no spaces).</summary>
        [SerializeField] private string actorName = "Actor";

        /// <summary>Walking speed in world units per second for objects without a PlayerMotor.</summary>
        [SerializeField] private float walkSpeed = 2.8f;

        /// <summary>
        /// If true, where cutscenes leave this actor is remembered (ActorStateStore) and applied again when the scene
        /// reloads. Turn it off for the player, whose position comes from the spawn point instead.
        /// </summary>
        [SerializeField] private bool persistState = true;

        /// <summary>How close (world units) the actor must get to the target to count as arrived.</summary>
        private const float ArrivalDistance = 0.08f;

        /// <summary>All active actors by name.</summary>
        private static readonly Dictionary<string, CutsceneActor> Actors =
            new Dictionary<string, CutsceneActor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The motor of a player-style actor (null for simple objects).</summary>
        private PlayerMotor motor;

        /// <summary>Facing and walk animation of an NPC character (null for the player and for plain blocks).</summary>
        private NpcFacing npcFacing;

        /// <summary>The facing of a player-style actor (null for simple objects).</summary>
        private PlayerFacing facing;

        /// <summary>Clears the registry when Play mode starts (static data can survive between Play sessions).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Actors.Clear();
        }

        /// <summary>Finds an actor by name.</summary>
        /// <param name="name">The actor name used in the command.</param>
        /// <returns>The actor, or null if none is active with that name.</returns>
        public static CutsceneActor Find(string name)
        {
            Actors.TryGetValue(name, out CutsceneActor actor);
            return actor;
        }

        /// <summary>Caches the optional player components.</summary>
        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            facing = GetComponent<PlayerFacing>();
            npcFacing = GetComponent<NpcFacing>();
        }

        /// <summary>Puts the actor where earlier cutscenes left it (only when this actor persists its state).</summary>
        private void Start()
        {
            if (!persistState)
            {
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            if (ActorStateStore.TryGetPosition(sceneName, actorName, out Vector2 position))
            {
                transform.position = new Vector3(position.x, position.y, transform.position.z);
                Physics2D.SyncTransforms();
            }

            if (facing != null && ActorStateStore.TryGetFacing(sceneName, actorName, out Vector2 savedFacing))
            {
                facing.SetDirection(savedFacing);
            }
            else if (npcFacing != null && ActorStateStore.TryGetFacing(sceneName, actorName, out Vector2 savedNpcFacing))
            {
                npcFacing.Face(savedNpcFacing);
            }
        }

        /// <summary>Registers the actor under its name.</summary>
        private void OnEnable()
        {
            Actors[actorName] = this;
        }

        /// <summary>Removes the actor from the registry.</summary>
        private void OnDisable()
        {
            if (Actors.TryGetValue(actorName, out CutsceneActor registered) && registered == this)
            {
                Actors.Remove(actorName);
            }
        }

        /// <summary>Walks to a world position and ends when it arrives (or gets stuck for too long).</summary>
        /// <param name="target">Target position in world units (the feet position of the actor).</param>
        /// <param name="permanent">True to remember the final position after scene changes; false for a temporary move.</param>
        /// <returns>A coroutine that ends on arrival.</returns>
        public IEnumerator MoveTo(Vector2 target, bool permanent)
        {
            float distance = Vector2.Distance(transform.position, target);
            float timeout = distance / walkSpeed * 3f + 1f;
            float elapsed = 0f;

            while (elapsed < timeout && Vector2.Distance(transform.position, target) > ArrivalDistance)
            {
                Vector2 toTarget = target - (Vector2)transform.position;

                if (motor != null)
                {
                    // Feeding the motor an input makes the physics, facing and walk animation behave as if the player pressed it.
                    motor.SetScriptedInput(toTarget.normalized);
                }
                else
                {
                    // An NPC (or a plain block) has no motor: it is moved directly. An NPC also turns towards its
                    // target and plays the walk animation through its Animator.
                    if (npcFacing != null)
                    {
                        npcFacing.Face(toTarget);
                        npcFacing.SetWalking(true);
                    }

                    Vector2 next = Vector2.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
                    transform.position = new Vector3(next.x, next.y, transform.position.z);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (motor != null)
            {
                motor.ClearScriptedInput();
            }

            if (npcFacing != null)
            {
                npcFacing.SetWalking(false);
            }

            if (permanent && persistState)
            {
                ActorStateStore.SetPosition(SceneManager.GetActiveScene().name, actorName, transform.position);
            }
        }

        /// <summary>Turns the actor to a direction. Has no visible effect on objects without facing (blocks, signs).</summary>
        /// <param name="direction">Direction vector, for example (0, 1) for north.</param>
        /// <param name="permanent">True to remember the direction after scene changes; false for a temporary turn.</param>
        public void Face(Vector2 direction, bool permanent)
        {
            if (permanent && persistState)
            {
                ActorStateStore.SetFacing(SceneManager.GetActiveScene().name, actorName, direction);
            }

            if (facing != null)
            {
                facing.SetDirection(direction);
            }

            if (npcFacing != null)
            {
                npcFacing.Face(direction);
            }
        }
    }
}
