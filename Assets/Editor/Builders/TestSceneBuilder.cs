using System.Collections.Generic;
using Pathless.CameraRig;
using Pathless.Cutscene;
using Pathless.Dialogue;
using Pathless.Interaction;
using Pathless.Menu;
using Pathless.Npc;
using Pathless.SceneFlow;
using Pathless.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// UnityEditor also has a type named InteractionMode, so this alias tells the compiler to use ours.
using InteractionMode = Pathless.Interaction.InteractionMode;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the test scene "Test_Movement": ground, a small house with a 2-tile-high door, the player,
    /// one optional NPC block (press E) and one forced NPC block (walk into its zone).
    /// The scene starts as a copy of SampleScene, so it keeps the template's camera and Global Light 2D.
    /// Run it from Tools > Pathless > Build Test Scene (or through "Build Everything").
    /// Running it again REPLACES the scene file.
    /// </summary>
    public static class TestSceneBuilder
    {
        /// <summary>Scene used as the starting point (it already has a camera and a Global Light 2D).</summary>
        public const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>Folder where test scenes are saved.</summary>
        public const string TestScenesFolder = "Assets/Scenes/Test";

        /// <summary>Full path of the generated test scene.</summary>
        public const string TestScenePath = "Assets/Scenes/Test/Test_Movement.unity";

        /// <summary>Spawn id of the point in front of the house door (where the player appears when leaving the house).</summary>
        public const string HouseSpawnId = "house_door";

        /// <summary>Pixels per unit of the project; the camera is configured around it.</summary>
        private const int PixelsPerUnit = 32;

        /// <summary>Reference resolution of the game view (width in pixels).</summary>
        private const int ReferenceWidth = 640;

        /// <summary>Reference resolution of the game view (height in pixels).</summary>
        private const int ReferenceHeight = 360;

        /// <summary>World half-width in tiles: the ground covers x from -20 to 19.</summary>
        private const int HalfWidth = 20;

        /// <summary>World half-height in tiles: the ground covers y from -15 to 14.</summary>
        private const int HalfHeight = 15;

        /// <summary>Sorting order of the ground tilemap (behind everything).</summary>
        private const int GroundSortingOrder = -100;

        /// <summary>Sorting order of the wall tilemap (above the ground, below characters).</summary>
        private const int WallSortingOrder = -90;

        /// <summary>Menu entry that builds only the scene (prefabs, tiles and animations must already exist).</summary>
        [MenuItem("Tools/Pathless/Build Test Scene")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>
        /// Creates the scene, fills it and saves it. The prefabs and Tile assets are loaded from disk
        /// right after the scene is opened (opening a scene unloads unused assets from memory).
        /// </summary>
        public static void Build()
        {
            EditorAssetUtility.EnsureFolder(TestScenesFolder);

            // Start from a fresh copy of the template scene (camera + Global Light 2D).
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!AssetDatabase.CopyAsset(TemplateScenePath, TestScenePath))
            {
                Debug.LogError("TestSceneBuilder: could not copy " + TemplateScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

            // Load the assets AFTER opening the scene, otherwise the references could already be unloaded (empty).
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabBuilder.PlayerPrefabPath);
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabBuilder.NpcBlockPrefabPath);
            Dictionary<string, Tile> tiles = TestTilesBuilder.LoadAll();
            GameObject dialoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUiBuilder.DialoguePrefabPath);
            DialogueSequence optionalSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.OptionalId);
            DialogueSequence forcedSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.ForcedId);
            GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabBuilder.SignPrefabPath);
            DialogueSequence signSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.SignId);
            GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuUiBuilder.MenuPrefabPath);
            GameObject cutscenePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneSystemBuilder.PrefabPath);
            GameObject violetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VioletPrefabBuilder.PrefabPath);
            DialogueSequence violetSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.VioletId);
            GameObject katePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KatePrefabBuilder.PrefabPath);
            DialogueSequence kateSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.KateId);
            GameObject alicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AlicePrefabBuilder.PrefabPath);
            DialogueSequence aliceSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.AliceId);
            GameObject gabePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GabePrefabBuilder.PrefabPath);
            DialogueSequence gabeSequence = DialogueTestDataBuilder.LoadSequence(DialogueTestDataBuilder.GabeId);

            if (playerPrefab == null || blockPrefab == null || tiles.Count == 0
                || dialoguePrefab == null || optionalSequence == null || forcedSequence == null
                || signPrefab == null || signSequence == null || menuPrefab == null || cutscenePrefab == null
                || violetPrefab == null || violetSequence == null || katePrefab == null || kateSequence == null
                || alicePrefab == null || aliceSequence == null || gabePrefab == null || gabeSequence == null)
            {
                Debug.LogError("TestSceneBuilder: prefabs, tiles or dialogue assets are missing. Use 'Build Everything' instead.");
                return;
            }

            BuildTilemaps(tiles);
            BuildWorldBounds();
            GameObject player = SpawnPlayer(playerPrefab, scene);
            DialogueRunner runner = SpawnDialogueSystem(dialoguePrefab, scene, player);
            SpawnMenuSystem(menuPrefab, scene, player, runner);
            SpawnTestBlocks(blockPrefab, scene, runner, optionalSequence, forcedSequence);
            SpawnSign(signPrefab, scene, runner, signSequence);
            SpawnNpc(violetPrefab, "Violet", new Vector3(-1.5f, 1.5f, 0f), scene, runner, violetSequence);
            SpawnNpc(katePrefab, "Kate", new Vector3(1.5f, 1.5f, 0f), scene, runner, kateSequence);
            SpawnNpc(alicePrefab, "Alice", new Vector3(-4.5f, 1.5f, 0f), scene, runner, aliceSequence);
            SpawnNpc(gabePrefab, "Gabe", new Vector3(4.5f, 2.5f, 0f), scene, runner, gabeSequence);
            ConfigureCamera(player.transform);
            GameObject cutsceneSystem = SpawnCutsceneSystem(cutscenePrefab, scene, player, runner);
            AddActor(GameObject.Find("NpcBlock_Optional"), "Elder");
            AddActor(GameObject.Find("NpcBlock_Forced"), "Messenger");

            // Icons above the characters: the Elder offers a quest (the icon disappears once it is accepted) and
            // Violet has something to talk about. The forced Messenger needs no icon (the cutscene starts by itself).
            InteractionIconBuilder.AddIcon(GameObject.Find("NpcBlock_Optional"), InteractionIconType.Quest, 1.5f, runner, "quest:find_bell");
            InteractionIconBuilder.AddIcon(GameObject.Find("Violet"), InteractionIconType.Talk, 2.2f, runner);
            InteractionIconBuilder.AddIcon(GameObject.Find("Kate"), InteractionIconType.Talk, 2.2f, runner);
            InteractionIconBuilder.AddIcon(GameObject.Find("Alice"), InteractionIconType.Talk, 2.2f, runner);
            InteractionIconBuilder.AddIcon(GameObject.Find("Gabe"), InteractionIconType.Talk, 2.2f, runner);

            // House door: walking into the zone loads the house interior. The spawn point is where the player
            // reappears when coming back out of the house.
            SceneFlowBuilder.AddSpawnPoint(HouseSpawnId, new Vector2(0f, 3.3f), Vector2.down);
            SceneFlowBuilder.AddTransitionZone("HouseDoorZone", new Vector2(0f, 4.5f), new Vector2(0.8f, 1f),
                "Test_House", TestHouseSceneBuilder.DoorSpawnId, "", cutsceneSystem.GetComponent<SceneLoader>());
            SceneFlowBuilder.AddArrival(player, cutsceneSystem, runner, null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestScenePath);
            Debug.Log("TestSceneBuilder: scene saved at " + TestScenePath);
        }

        /// <summary>Creates the Grid with the Ground and Walls tilemaps and paints the test map.</summary>
        /// <param name="tiles">The Tile assets by name.</param>
        private static void BuildTilemaps(Dictionary<string, Tile> tiles)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.AddComponent<Grid>();

            Tilemap ground = CreateTilemap(gridObject.transform, "Ground", GroundSortingOrder, false);
            Tilemap walls = CreateTilemap(gridObject.transform, "Walls", WallSortingOrder, true);

            // Grass everywhere, with a deterministic mix of variations so the map looks the same every build.
            for (int x = -HalfWidth; x < HalfWidth; x++)
            {
                for (int y = -HalfHeight; y < HalfHeight; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), tiles[PickGrassName(x, y)]);
                }
            }

            // Vertical dirt path (3 tiles wide) leading from the south up to the house door.
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -HalfHeight; y <= 1; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), tiles[PickDirtName(x, y)]);
                }
            }

            // Horizontal road crossing the map.
            for (int x = -HalfWidth + 1; x < HalfWidth - 1; x++)
            {
                for (int y = -4; y <= -3; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), tiles[PickDirtName(x, y)]);
                }
            }

            // Stone floor in front of the door.
            for (int x = -1; x <= 1; x++)
            {
                for (int y = 2; y <= 3; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), tiles["stone_floor"]);
                }
            }

            // House: 9 tiles wide, 3 tiles high (roof row, upper wall row, lower wall row).
            // The door is at x = 0 and uses the 2 wall rows (2 tiles = 64 pixels high); it has no collider.
            for (int x = -4; x <= 4; x++)
            {
                walls.SetTile(new Vector3Int(x, 6, 0), tiles["wall_cap"]);
                walls.SetTile(new Vector3Int(x, 5, 0), x == 0 ? tiles["door_top"] : tiles["wall_face_upper"]);
                walls.SetTile(new Vector3Int(x, 4, 0), x == 0 ? tiles["door_bottom"] : tiles["wall_face_lower"]);
            }
        }

        /// <summary>Creates one Tilemap object under the grid.</summary>
        /// <param name="parent">The Grid transform.</param>
        /// <param name="tilemapName">Name of the object (Ground or Walls).</param>
        /// <param name="sortingOrder">Draw order; lower numbers are drawn first (behind).</param>
        /// <param name="solid">True to add a collider so tiles with a collider type block the player.</param>
        /// <returns>The created Tilemap.</returns>
        public static Tilemap CreateTilemap(Transform parent, string tilemapName, int sortingOrder, bool solid)
        {
            GameObject tilemapObject = new GameObject(tilemapName);
            tilemapObject.transform.SetParent(parent, false);

            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = sortingOrder;

            if (solid)
            {
                tilemapObject.AddComponent<TilemapCollider2D>();
            }

            return tilemap;
        }

        /// <summary>Picks a grass variation for a cell using a fixed hash, so the result is repeatable.</summary>
        /// <param name="x">Cell x.</param>
        /// <param name="y">Cell y.</param>
        /// <returns>The tile name: mostly plain grass, sometimes tufts, rarely flowers.</returns>
        private static string PickGrassName(int x, int y)
        {
            int value = Hash(x, y) % 20;
            if (value < 8) return "grass_a";
            if (value < 14) return "grass_b";
            if (value < 18) return "grass_tuft";
            return "grass_flowers";
        }

        /// <summary>Picks one of the two dirt variations for a cell using a fixed hash.</summary>
        /// <param name="x">Cell x.</param>
        /// <param name="y">Cell y.</param>
        /// <returns>The tile name "dirt_a" or "dirt_b".</returns>
        private static string PickDirtName(int x, int y)
        {
            return Hash(x, y) % 2 == 0 ? "dirt_a" : "dirt_b";
        }

        /// <summary>Simple deterministic hash of a cell position (always returns a non-negative number).</summary>
        /// <param name="x">Cell x.</param>
        /// <param name="y">Cell y.</param>
        /// <returns>A pseudo random but repeatable non-negative integer.</returns>
        private static int Hash(int x, int y)
        {
            unchecked
            {
                int h = (x * 73856093) ^ (y * 19349663);
                return h & 0x7FFFFFFF;
            }
        }

        /// <summary>Creates invisible walls around the map so the player cannot walk into the empty void.</summary>
        private static void BuildWorldBounds()
        {
            GameObject bounds = new GameObject("WorldBounds");

            // Four thick boxes (1 unit thick) just outside the ground area.
            AddBound(bounds, new Vector2(-HalfWidth - 0.5f, 0f), new Vector2(1f, HalfHeight * 2f + 2f));
            AddBound(bounds, new Vector2(HalfWidth + 0.5f, 0f), new Vector2(1f, HalfHeight * 2f + 2f));
            AddBound(bounds, new Vector2(0f, HalfHeight + 0.5f), new Vector2(HalfWidth * 2f + 2f, 1f));
            AddBound(bounds, new Vector2(0f, -HalfHeight - 0.5f), new Vector2(HalfWidth * 2f + 2f, 1f));
        }

        /// <summary>Adds one solid box collider to the bounds object.</summary>
        /// <param name="target">The object that receives the collider.</param>
        /// <param name="center">Center of the box in world units.</param>
        /// <param name="size">Size of the box in world units.</param>
        private static void AddBound(GameObject target, Vector2 center, Vector2 size)
        {
            BoxCollider2D box = target.AddComponent<BoxCollider2D>();
            box.offset = center;
            box.size = size;
        }

        /// <summary>Places the Player prefab on the dirt path, south of the house.</summary>
        /// <param name="playerPrefab">The Player prefab.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <returns>The scene instance of the player.</returns>
        public static GameObject SpawnPlayer(GameObject playerPrefab, Scene scene)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0.5f, -1f, 0f);
            return player;
        }

        /// <summary>
        /// Places the DialogueSystem prefab and tells its player lock which player to freeze during dialogues.
        /// </summary>
        /// <param name="dialoguePrefab">The DialogueSystem prefab.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <param name="player">The player instance in the scene.</param>
        /// <returns>The dialogue runner of the placed system.</returns>
        public static DialogueRunner SpawnDialogueSystem(GameObject dialoguePrefab, Scene scene, GameObject player)
        {
            GameObject system = (GameObject)PrefabUtility.InstantiatePrefab(dialoguePrefab, scene);
            DialoguePlayerLock playerLock = system.GetComponent<DialoguePlayerLock>();
            EditorAssetUtility.SetReference(playerLock, "playerMotor", player.GetComponent<PlayerMotor>());
            DialogueRunner runner = system.GetComponent<DialogueRunner>();

            // The list of options for @choice lines lives in its own prefab; the runner needs a link to it.
            GameObject choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChoiceUiBuilder.PrefabPath);
            if (choicePrefab != null)
            {
                GameObject choiceSystem = (GameObject)PrefabUtility.InstantiatePrefab(choicePrefab, scene);
                EditorAssetUtility.SetReference(runner, "choiceView", choiceSystem.GetComponent<ChoiceView>());
            }
            else
            {
                Debug.LogWarning("TestSceneBuilder: ChoiceSystem prefab not found, @choice will not show options. Use 'Build Everything'.");
            }

            return runner;
        }

        /// <summary>Places the two test blocks: one optional (press E) and one forced (walk into its zone).</summary>
        /// <param name="blockPrefab">The NPC block prefab.</param>
        /// <param name="scene">The scene to place them in.</param>
        /// <param name="runner">The dialogue runner the blocks will use.</param>
        /// <param name="optionalSequence">Conversation of the optional block.</param>
        /// <param name="forcedSequence">Conversation of the forced block.</param>
        private static void SpawnTestBlocks(GameObject blockPrefab, Scene scene, DialogueRunner runner,
            DialogueSequence optionalSequence, DialogueSequence forcedSequence)
        {
            // Optional block: next to the path. Face it and press E (or X / A on a gamepad).
            GameObject optionalBlock = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, scene);
            optionalBlock.name = "NpcBlock_Optional";
            optionalBlock.transform.position = new Vector3(3.5f, 0f, 0f);
            ConfigureBlock(optionalBlock, InteractionMode.Optional, optionalSequence, runner);

            // Forced block: on the road. Walking into the trigger zone around it starts the interaction.
            GameObject forcedBlock = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, scene);
            forcedBlock.name = "NpcBlock_Forced";
            forcedBlock.transform.position = new Vector3(-4.5f, -6f, 0f);
            ConfigureBlock(forcedBlock, InteractionMode.Forced, forcedSequence, runner);

            // Tint it so it is easy to tell apart from the optional block.
            forcedBlock.GetComponent<SpriteRenderer>().color = new Color(1f, 0.8f, 0.5f, 1f);

            // The zone is a child with a trigger collider (3 x 3 units) around the block.
            GameObject zoneObject = new GameObject("ForcedZone");
            zoneObject.transform.SetParent(forcedBlock.transform, false);
            zoneObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            BoxCollider2D zoneCollider = zoneObject.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector2(3f, 3f);
            zoneObject.AddComponent<InteractionZone>();
        }

        /// <summary>Sets the mode, conversation and runner of a test block instance.</summary>
        /// <param name="block">The block instance in the scene.</param>
        /// <param name="mode">Optional or Forced.</param>
        /// <param name="sequence">The conversation the block plays.</param>
        /// <param name="runner">The dialogue runner that plays it.</param>
        private static void ConfigureBlock(GameObject block, InteractionMode mode, DialogueSequence sequence, DialogueRunner runner)
        {
            DialogueInteractable interactable = block.GetComponent<DialogueInteractable>();
            EditorAssetUtility.SetEnum(interactable, "mode", (int)mode);
            EditorAssetUtility.SetReference(interactable, "sequence", sequence);
            EditorAssetUtility.SetReference(interactable, "runner", runner);
        }

        /// <summary>
        /// Places the MenuSystem prefab, tells its player lock which player to freeze, and gives the menu the
        /// dialogue runner so the menu cannot open during a dialogue.
        /// </summary>
        /// <param name="menuPrefab">The MenuSystem prefab.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <param name="player">The player instance in the scene.</param>
        /// <param name="runner">The dialogue runner of the scene.</param>
        public static void SpawnMenuSystem(GameObject menuPrefab, Scene scene, GameObject player, DialogueRunner runner)
        {
            GameObject system = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, scene);
            EditorAssetUtility.SetReference(system.GetComponent<MenuPlayerLock>(), "playerMotor", player.GetComponent<PlayerMotor>());
            EditorAssetUtility.SetReference(system.GetComponent<MenuController>(), "dialogueRunner", runner);

            // Full menu screens (Status, Quests...): their router needs the menu and its input reader of this scene.
            GameObject screensPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuScreensBuilder.PrefabPath);
            if (screensPrefab != null)
            {
                GameObject screens = (GameObject)PrefabUtility.InstantiatePrefab(screensPrefab, scene);
                MenuScreenRouter router = screens.GetComponent<MenuScreenRouter>();
                EditorAssetUtility.SetReference(router, "controller", system.GetComponent<MenuController>());
                EditorAssetUtility.SetReference(router, "input", system.GetComponent<MenuInputReader>());
                EditorAssetUtility.SetReference(system.GetComponent<MenuOptionHandler>(), "screenRouter", router);
            }
            else
            {
                Debug.LogWarning("TestSceneBuilder: MenuScreens prefab not found, Status and Quests will not open. Use 'Build Everything'.");
            }
        }

        /// <summary>
        /// Places the CutsceneSystem, links it to the dialogue runner and the camera, and makes the player and the two
        /// blocks cutscene actors ("Player", "Elder", "Messenger") so @move/@face/@camera commands can find them.
        /// Must run after ConfigureCamera (it needs the CameraFollow). The NPC actors are added by the caller.
        /// </summary>
        /// <param name="cutscenePrefab">The CutsceneSystem prefab.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <param name="player">The player instance.</param>
        /// <param name="runner">The dialogue runner that will send @commands to the director.</param>
        /// <returns>The CutsceneSystem instance.</returns>
        public static GameObject SpawnCutsceneSystem(GameObject cutscenePrefab, Scene scene, GameObject player, DialogueRunner runner)
        {
            GameObject system = (GameObject)PrefabUtility.InstantiatePrefab(cutscenePrefab, scene);
            CutsceneDirector director = system.GetComponent<CutsceneDirector>();
            CutsceneCamera cutsceneCamera = system.GetComponent<CutsceneCamera>();

            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            EditorAssetUtility.SetReference(cutsceneCamera, "follow", camera.GetComponent<CameraFollow>());
            EditorAssetUtility.SetReference(director, "runner", runner);
            EditorAssetUtility.SetReference(runner, "commandExecutor", director);

            AddActor(player, "Player", false);
            return system;
        }

        /// <summary>Adds a CutsceneActor with the given name to an object.</summary>
        /// <param name="target">The object to make controllable.</param>
        /// <param name="actorName">The name used in cutscene commands.</param>
        /// <param name="persistState">True to remember where cutscenes leave the actor across scene changes (false for the player).</param>
        private static void AddActor(GameObject target, string actorName, bool persistState = true)
        {
            if (target == null)
            {
                Debug.LogError($"TestSceneBuilder: cannot add actor '{actorName}', object not found.");
                return;
            }

            CutsceneActor actor = target.AddComponent<CutsceneActor>();
            EditorAssetUtility.SetString(actor, "actorName", actorName);

            SerializedObject serialized = new SerializedObject(actor);
            serialized.FindProperty("persistState").boolValue = persistState;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Places a standing NPC character (Violet, Kate...) looking south (towards the screen). Face it and press E
        /// to talk; it turns to look at the player when the conversation starts.
        /// </summary>
        /// <param name="npcPrefab">The character's prefab.</param>
        /// <param name="npcName">Name of the object in the scene (also used to find it later).</param>
        /// <param name="position">Where to place it.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <param name="runner">The dialogue runner.</param>
        /// <param name="sequence">The character's conversation.</param>
        private static void SpawnNpc(GameObject npcPrefab, string npcName, Vector3 position, Scene scene,
            DialogueRunner runner, DialogueSequence sequence)
        {
            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, scene);
            npc.name = npcName;
            npc.transform.position = position;
            ConfigureBlock(npc, InteractionMode.Optional, sequence, runner);
        }

        /// <summary>Places a test sign next to the path. Face it and press E to read it.</summary>
        /// <param name="signPrefab">The sign prefab.</param>
        /// <param name="scene">The scene to place it in.</param>
        /// <param name="runner">The dialogue runner.</param>
        /// <param name="sequence">The sign text (narration lines).</param>
        private static void SpawnSign(GameObject signPrefab, Scene scene, DialogueRunner runner, DialogueSequence sequence)
        {
            GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab, scene);
            sign.name = "Sign_Welcome";
            sign.transform.position = new Vector3(-2.5f, 0f, 0f);
            ConfigureBlock(sign, InteractionMode.Optional, sequence, runner);
        }

        /// <summary>
        /// Sets up the scene camera: orthographic size for the 640x360 reference at 32 PPU,
        /// the Pixel Perfect Camera component and the follow script targeting the player.
        /// </summary>
        /// <param name="target">The player transform to follow.</param>
        public static void ConfigureCamera(Transform target)
        {
            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("TestSceneBuilder: no camera found in the scene.");
                return;
            }

            // Half of the reference height divided by pixels per unit: 360 / 2 / 32 = 5.625 units.
            camera.orthographic = true;
            camera.orthographicSize = ReferenceHeight / 2f / PixelsPerUnit;

            PixelPerfectCamera pixelPerfect = camera.gameObject.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();
            }

            pixelPerfect.assetsPPU = PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;

            CameraFollow follow = camera.gameObject.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            EditorAssetUtility.SetReference(follow, "target", target);
        }
    }
}
