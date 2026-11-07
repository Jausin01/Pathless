using System.Collections.Generic;
using Pathless.Cutscene;
using Pathless.Dialogue;
using Pathless.Player;
using Pathless.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the second test scene "Test_House": a small stone room reached through the house door of Test_Movement.
    /// It exists to test scene changes: arriving at a spawn point, continuing a cutscene (conversation "house_intro"
    /// of Assets/Dialogue/Test_House.dialogue) and walking back out through the door at the bottom.
    /// Running it again REPLACES the scene file. Menu: Tools > Pathless > Build Test House Scene.
    /// </summary>
    public static class TestHouseSceneBuilder
    {
        /// <summary>Full path of the generated scene.</summary>
        public const string ScenePath = "Assets/Scenes/Test/Test_House.unity";

        /// <summary>Path of the dialogue file with the arrival cutscene.</summary>
        private const string DialoguePath = "Assets/Dialogue/Test_House.dialogue";

        /// <summary>Spawn id of the entrance (the exterior scene sends the player here).</summary>
        public const string DoorSpawnId = "door";

        /// <summary>Menu entry to rebuild only this scene.</summary>
        [MenuItem("Tools/Pathless/Build Test House Scene")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the room scene and saves it.</summary>
        public static void Build()
        {
            EditorAssetUtility.EnsureFolder(TestSceneBuilder.TestScenesFolder);
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(TestSceneBuilder.TemplateScenePath, ScenePath))
            {
                Debug.LogError("TestHouseSceneBuilder: could not copy " + TestSceneBuilder.TemplateScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Load assets AFTER opening the scene (opening a scene unloads unused assets from memory).
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabBuilder.PlayerPrefabPath);
            Dictionary<string, Tile> tiles = TestTilesBuilder.LoadAll();
            GameObject dialoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUiBuilder.DialoguePrefabPath);
            GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuUiBuilder.MenuPrefabPath);
            GameObject cutscenePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneSystemBuilder.PrefabPath);
            DialogueScript arrivalScript = AssetDatabase.LoadAssetAtPath<DialogueScript>(DialoguePath);

            if (playerPrefab == null || tiles.Count == 0 || dialoguePrefab == null || menuPrefab == null
                || cutscenePrefab == null || arrivalScript == null)
            {
                Debug.LogError("TestHouseSceneBuilder: prefabs, tiles or " + DialoguePath + " are missing. Use 'Build Everything' instead.");
                return;
            }

            BuildRoom(tiles);

            GameObject player = TestSceneBuilder.SpawnPlayer(playerPrefab, scene);
            player.transform.position = new Vector3(0f, -3.2f, 0f);
            DialogueRunner runner = TestSceneBuilder.SpawnDialogueSystem(dialoguePrefab, scene, player);
            TestSceneBuilder.SpawnMenuSystem(menuPrefab, scene, player, runner);
            TestSceneBuilder.ConfigureCamera(player.transform);
            GameObject cutsceneSystem = TestSceneBuilder.SpawnCutsceneSystem(cutscenePrefab, scene, player, runner);
            AudioBuilder.SetSceneTrack(cutsceneSystem, "House");

            // Entrance point, exit door and the arrival handler.
            SceneFlowBuilder.AddSpawnPoint(DoorSpawnId, new Vector2(0f, -3.2f), Vector2.up);
            SceneFlowBuilder.AddTransitionZone("ExitDoorZone", new Vector2(0f, -4.5f), new Vector2(0.8f, 0.8f),
                "Test_Movement", TestSceneBuilder.HouseSpawnId, "", cutsceneSystem.GetComponent<SceneLoader>());
            SceneFlowBuilder.AddArrival(player, cutsceneSystem, runner, arrivalScript);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("TestHouseSceneBuilder: scene saved at " + ScenePath);
        }

        /// <summary>
        /// Paints the room: stone floor (x -5 to 4, y -4 to 1), three wall rows on top, wall columns on the sides
        /// and a bottom wall with a door gap at x = 0.
        /// </summary>
        /// <param name="tiles">The Tile assets by name.</param>
        private static void BuildRoom(Dictionary<string, Tile> tiles)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.AddComponent<Grid>();

            Tilemap ground = TestSceneBuilder.CreateTilemap(gridObject.transform, "Ground", -100, false);
            Tilemap walls = TestSceneBuilder.CreateTilemap(gridObject.transform, "Walls", -90, true);

            for (int x = -5; x <= 4; x++)
            {
                for (int y = -4; y <= 1; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), tiles["stone_floor"]);
                }
            }

            for (int x = -6; x <= 5; x++)
            {
                walls.SetTile(new Vector3Int(x, 4, 0), tiles["wall_cap"]);
                walls.SetTile(new Vector3Int(x, 3, 0), tiles["wall_face_upper"]);
                walls.SetTile(new Vector3Int(x, 2, 0), tiles["wall_face_lower"]);

                // Bottom wall, with the door tile (no collider) at x = 0.
                walls.SetTile(new Vector3Int(x, -5, 0), x == 0 ? tiles["door_bottom"] : tiles["wall_face_lower"]);
            }

            for (int y = -4; y <= 1; y++)
            {
                walls.SetTile(new Vector3Int(-6, y, 0), tiles["wall_face_lower"]);
                walls.SetTile(new Vector3Int(5, y, 0), tiles["wall_face_lower"]);
            }
        }
    }
}
