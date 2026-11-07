using System.Collections.Generic;
using Pathless.Cutscene;
using Pathless.Dialogue;
using Pathless.Interaction;
using Pathless.Player;
using Pathless.SceneFlow;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Helpers that add the scene-change pieces (spawn points, door zones, the arrival handler) to the scene
    /// that is currently open, and that register the test scenes in the Build Settings (a scene that is not
    /// in the Build Settings cannot be loaded at runtime).
    /// </summary>
    public static class SceneFlowBuilder
    {
        /// <summary>Adds an empty object with a SceneSpawnPoint.</summary>
        /// <param name="spawnId">Id other scenes use to find this point.</param>
        /// <param name="position">World position of the player's feet.</param>
        /// <param name="facing">Direction the player faces on arrival.</param>
        public static void AddSpawnPoint(string spawnId, Vector2 position, Vector2 facing)
        {
            GameObject point = new GameObject("Spawn_" + spawnId);
            point.transform.position = new Vector3(position.x, position.y, 0f);
            SceneSpawnPoint component = point.AddComponent<SceneSpawnPoint>();
            EditorAssetUtility.SetString(component, "spawnId", spawnId);

            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("facing").vector2Value = facing;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds a door/exit: a trigger area that loads another scene when the player walks into it.</summary>
        /// <param name="zoneName">Object name.</param>
        /// <param name="center">World position of the area center.</param>
        /// <param name="size">Size of the trigger area in world units.</param>
        /// <param name="targetScene">Scene to load (file name without ".unity").</param>
        /// <param name="targetSpawnId">Spawn id in that scene.</param>
        /// <param name="continueId">Conversation id to continue there (can be empty).</param>
        /// <param name="loader">The scene loader of this scene.</param>
        public static void AddTransitionZone(string zoneName, Vector2 center, Vector2 size, string targetScene,
            string targetSpawnId, string continueId, SceneLoader loader)
        {
            GameObject zone = new GameObject(zoneName);
            zone.transform.position = new Vector3(center.x, center.y, 0f);

            BoxCollider2D box = zone.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;

            zone.AddComponent<InteractionZone>();
            SceneTransitionZone transition = zone.AddComponent<SceneTransitionZone>();
            EditorAssetUtility.SetString(transition, "targetScene", targetScene);
            EditorAssetUtility.SetString(transition, "targetSpawnId", targetSpawnId);
            EditorAssetUtility.SetString(transition, "continueSequenceId", continueId);
            EditorAssetUtility.SetReference(transition, "loader", loader);
        }

        /// <summary>Adds the object that handles arriving in this scene (places the player, fades in, continues a cutscene).</summary>
        /// <param name="player">The player in the scene.</param>
        /// <param name="cutsceneSystem">The CutsceneSystem instance (it holds the fader).</param>
        /// <param name="runner">The dialogue runner.</param>
        /// <param name="arrivalScript">The .dialogue file of this scene (can be null).</param>
        public static void AddArrival(GameObject player, GameObject cutsceneSystem, DialogueRunner runner, DialogueScript arrivalScript)
        {
            GameObject entry = new GameObject("SceneArrival");
            SceneArrival arrival = entry.AddComponent<SceneArrival>();
            EditorAssetUtility.SetReference(arrival, "player", player.GetComponent<PlayerMotor>());
            EditorAssetUtility.SetReference(arrival, "fader", cutsceneSystem.GetComponentInChildren<ScreenFader>());
            EditorAssetUtility.SetReference(arrival, "runner", runner);
            EditorAssetUtility.SetReference(arrival, "arrivalScript", arrivalScript);
        }

        /// <summary>Puts the given scenes in the Build Settings (keeps scenes that were already there).</summary>
        /// <param name="scenePaths">Asset paths of the scenes to register.</param>
        public static void RegisterScenes(params string[] scenePaths)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in scenePaths)
            {
                if (!scenes.Exists(existing => existing.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
