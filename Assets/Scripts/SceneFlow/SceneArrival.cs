using System.Collections;
using Pathless.Cutscene;
using Pathless.Dialogue;
using Pathless.Player;
using UnityEngine;

namespace Pathless.SceneFlow
{
    /// <summary>
    /// Runs when a scene starts after a transition: puts the player on the requested spawn point, fades the
    /// screen back in and, if the transition asked for it, starts the conversation that continues the cutscene.
    /// When the scene is started directly (Play button) there is no pending transition and it does nothing.
    /// Usage: one object per scene with this script; Arrival Script is the .dialogue file of the scene (optional).
    /// </summary>
    public class SceneArrival : MonoBehaviour
    {
        /// <summary>Fade-in time in seconds.</summary>
        private const float FadeSeconds = 0.6f;

        /// <summary>The player to place.</summary>
        [SerializeField] private PlayerMotor player;

        /// <summary>The fader that starts black and fades in.</summary>
        [SerializeField] private ScreenFader fader;

        /// <summary>The dialogue runner that plays the continued conversation.</summary>
        [SerializeField] private DialogueRunner runner;

        /// <summary>The .dialogue file of this scene, where "continue" conversations are searched by id.</summary>
        [SerializeField] private DialogueScript arrivalScript;

        /// <summary>Handles the transition, if there is one.</summary>
        private IEnumerator Start()
        {
            if (!SceneTransitionData.HasPending)
            {
                yield break;
            }

            string spawnId = SceneTransitionData.SpawnId;
            string continueId = SceneTransitionData.ContinueId;
            SceneTransitionData.Clear();

            // Freeze the player while the screen is black.
            player.SetMovementEnabled(false);
            PlaceAtSpawn(spawnId);

            // Wait one frame so the camera snaps to the new position before the fade-in starts.
            yield return null;
            yield return StartCoroutine(fader.Fade(false, FadeSeconds));

            DialogueSequence sequence = FindSequence(continueId);
            if (sequence != null)
            {
                // The dialogue lock takes over the player now and releases it when the conversation ends.
                runner.StartDialogue(sequence);
            }
            else
            {
                player.SetMovementEnabled(true);
            }
        }

        /// <summary>Moves the player to the spawn point with the given id and turns it to the point's facing.</summary>
        /// <param name="spawnId">The spawn id to look for (empty = do nothing).</param>
        private void PlaceAtSpawn(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId))
            {
                return;
            }

            foreach (SceneSpawnPoint point in FindObjectsByType<SceneSpawnPoint>())
            {
                if (point.SpawnId != spawnId)
                {
                    continue;
                }

                player.transform.position = new Vector3(point.transform.position.x, point.transform.position.y, player.transform.position.z);
                Physics2D.SyncTransforms();

                PlayerFacing facing = player.GetComponent<PlayerFacing>();
                if (facing != null)
                {
                    facing.SetDirection(point.Facing);
                }

                return;
            }

            Debug.LogWarning($"SceneArrival: no SceneSpawnPoint with id '{spawnId}' in this scene.", this);
        }

        /// <summary>Finds a conversation by id in the arrival script.</summary>
        /// <param name="sequenceId">The id written in the [id | tag] header (empty = none).</param>
        /// <returns>The conversation, or null if none was requested or it does not exist (a warning is logged).</returns>
        private DialogueSequence FindSequence(string sequenceId)
        {
            if (string.IsNullOrEmpty(sequenceId))
            {
                return null;
            }

            if (arrivalScript != null)
            {
                foreach (DialogueSequence sequence in arrivalScript.Sequences)
                {
                    if (sequence != null && sequence.name == sequenceId)
                    {
                        return sequence;
                    }
                }
            }

            Debug.LogWarning($"SceneArrival: conversation '{sequenceId}' not found in this scene's arrival script.", this);
            return null;
        }
    }
}
