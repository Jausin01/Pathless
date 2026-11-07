using Pathless.Npc;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the prefab of a standing NPC character: a sprite animated by the character's own Animator Controller,
    /// a solid collider at the feet, a DialogueInteractable (talk with E) and NpcFacing (which way it looks).
    /// The character's animations must be built first (for example by VioletAnimationBuilder).
    /// The prefab is replaced each time it is built. VioletPrefabBuilder and KatePrefabBuilder only say which character.
    /// </summary>
    public static class CharacterPrefabBuilder
    {
        /// <summary>Path of the prefab of a character.</summary>
        /// <param name="character">Character name, for example "Violet".</param>
        public static string PrefabPathOf(string character) => $"Assets/Prefabs/Characters/{character}.prefab";

        /// <summary>Builds the prefab of one character.</summary>
        /// <param name="character">Character name (also the art folder, e.g. "Violet").</param>
        /// <param name="controllerPath">Path of the character's Animator Controller.</param>
        /// <returns>The prefab, or null if the controller or sprite is missing.</returns>
        public static GameObject Build(string character, string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Art/Characters/{character}/Frames/south/{character}_Walk_south_00.png");
            if (controller == null || sprite == null)
            {
                Debug.LogError($"CharacterPrefabBuilder: {controllerPath} or the {character} sprite is missing. Build the animations first.");
                return null;
            }

            EditorAssetUtility.EnsureFolder("Assets/Prefabs/Characters");

            GameObject npc = new GameObject(character);

            SpriteRenderer spriteRenderer = npc.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            // Sort by the feet, like the player, so whoever stands lower on the screen draws in front.
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

            Animator animator = npc.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            // Solid collider at the feet (same size as the player's), so the player cannot walk through her.
            BoxCollider2D feetCollider = npc.AddComponent<BoxCollider2D>();
            feetCollider.size = new Vector2(0.625f, 0.25f);
            feetCollider.offset = new Vector2(0f, 0.125f);

            npc.AddComponent<Pathless.Dialogue.DialogueInteractable>();
            npc.AddComponent<NpcFacing>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(npc, PrefabPathOf(character));
            Object.DestroyImmediate(npc);
            Debug.Log("CharacterPrefabBuilder: prefab saved at " + PrefabPathOf(character));
            return prefab;
        }
    }
}
