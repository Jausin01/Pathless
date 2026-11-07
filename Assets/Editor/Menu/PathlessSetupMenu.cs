using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pathless.EditorTools
{
    /// <summary>
    /// One menu entry that runs every builder in the right order:
    /// reimport art, build animations, build tiles, build the dialogue system, build prefabs, build the test scene.
    /// Each step also has its own menu entry (see the builder classes) if you only need one of them.
    /// </summary>
    public static class PathlessSetupMenu
    {
        /// <summary>
        /// Runs the whole test setup. Safe to run more than once: generated assets are replaced.
        /// Menu: Tools > Pathless > Build Everything (Test Setup).
        /// </summary>
        [MenuItem("Tools/Pathless/Build Everything (Test Setup)")]
        public static void BuildEverything()
        {
            // Step 1: reimport the art so the pixel art rules (32 PPU, Point filter, pivots) are applied
            // even if the files were imported before the importer script was compiled.
            AssetDatabase.ImportAsset("Assets/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            // Step 2: animation clips and the Animator Controller.
            AnimatorController controller = PlayerAnimationBuilder.Build();
            if (controller == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, animations could not be built.");
                return;
            }

            // Violet's animations (a separate controller with the same structure).
            if (VioletAnimationBuilder.Build() == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, Violet's animations could not be built.");
                return;
            }

            // Kate's animations.
            if (KateAnimationBuilder.Build() == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, Kate's animations could not be built.");
                return;
            }

            // Alice's animations.
            if (AliceAnimationBuilder.Build() == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, Alice's animations could not be built.");
                return;
            }

            // Gabe's animations.
            if (GabeAnimationBuilder.Build() == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, Gabe's animations could not be built.");
                return;
            }

            // Step 3: Tile assets.
            Dictionary<string, Tile> tiles = TestTilesBuilder.Build();
            if (tiles.Count == 0)
            {
                Debug.LogError("PathlessSetupMenu: stopped, tiles could not be built.");
                return;
            }

            // Step 3b: dialogue system (animations, test characters and conversations, UI prefab).
            // The UI and portrait art is reimported first so the 9-slice borders are applied.
            if (!DialogueAnimationBuilder.Build() || !StoryCharacterBuilder.Build() || !DialogueTestDataBuilder.Build() || !DialogueUiBuilder.Build())
            {
                Debug.LogError("PathlessSetupMenu: stopped, the dialogue system could not be built.");
                return;
            }

            // Step 3c: menu (animations, test definition, UI prefab).
            if (!MenuAnimationBuilder.Build() || !MenuUiBuilder.Build())
            {
                Debug.LogError("PathlessSetupMenu: stopped, the menu could not be built.");
                return;
            }

            // Step 3c-1: full menu screens (Status, Quests) and their test data. They reuse the menu row prefab.
            if (!MenuScreenAnimationBuilder.Build() || !MenuScreensBuilder.Build())
            {
                Debug.LogError("PathlessSetupMenu: stopped, the menu screens could not be built.");
                return;
            }

            // Step 3c-2: the option list of @choice (reuses the menu row prefab, so it must come after the menu).
            if (!ChoiceUiBuilder.Build())
            {
                Debug.LogError("PathlessSetupMenu: stopped, the choice list could not be built.");
                return;
            }

            // Step 3d: cutscene system (fade animations, CutsceneSystem prefab).
            if (!CutsceneAnimationBuilder.Build() || !CutsceneSystemBuilder.Build())
            {
                Debug.LogError("PathlessSetupMenu: stopped, the cutscene system could not be built.");
                return;
            }

            // Step 3d: the icon set (shown above characters).
            if (InteractionIconBuilder.Build() == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, the interaction icons could not be built.");
                return;
            }

            // Step 4: prefabs.
            GameObject playerPrefab = TestPrefabBuilder.BuildPlayerPrefab(controller);
            GameObject blockPrefab = TestPrefabBuilder.BuildNpcBlockPrefab();
            GameObject signPrefab = TestPrefabBuilder.BuildSignPrefab();
            GameObject violetPrefab = VioletPrefabBuilder.Build();
            GameObject katePrefab = KatePrefabBuilder.Build();
            GameObject alicePrefab = AlicePrefabBuilder.Build();
            GameObject gabePrefab = GabePrefabBuilder.Build();
            if (playerPrefab == null || blockPrefab == null || signPrefab == null || violetPrefab == null || katePrefab == null || alicePrefab == null || gabePrefab == null)
            {
                Debug.LogError("PathlessSetupMenu: stopped, prefabs could not be built.");
                return;
            }

            // Step 5: the test scene.
            TestSceneBuilder.Build();

            // Step 6: the house interior scene, then register both scenes so they can be loaded at runtime.
            TestHouseSceneBuilder.Build();
            SceneFlowBuilder.RegisterScenes(TestSceneBuilder.TestScenePath, TestHouseSceneBuilder.ScenePath);

            // Leave the main test scene open (building the house opened the interior).
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(TestSceneBuilder.TestScenePath);

            AssetDatabase.SaveAssets();
            Debug.Log("PathlessSetupMenu: done. Open Assets/Scenes/Test/Test_Movement.unity and press Play.");
        }
    }
}
