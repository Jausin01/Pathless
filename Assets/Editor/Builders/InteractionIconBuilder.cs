using Pathless.Dialogue;
using Pathless.Npc;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the InteractionIconSet asset from the icon art (Assets/Art/Icons/Icon_Talk, Icon_Quest and Icon_Save)
    /// and adds icons to characters in a scene. The set is updated in place, so scenes keep their references.
    /// Menu: Tools > Pathless > Build Interaction Icons.
    /// </summary>
    public static class InteractionIconBuilder
    {
        /// <summary>Where the icon set asset is saved.</summary>
        public const string SetPath = "Assets/Data/Npc/InteractionIconSet.asset";

        /// <summary>Folder of the icon art.</summary>
        private const string ArtFolder = "Assets/Art/Icons";

        /// <summary>Sorting order of the icon renderers, high so they draw above characters.</summary>
        private const int IconSortingOrder = 100;

        /// <summary>Menu entry that builds the set.</summary>
        [MenuItem("Tools/Pathless/Build Interaction Icons")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates or updates the icon set.</summary>
        /// <returns>The set, or null if an icon image is missing.</returns>
        public static InteractionIconSet Build()
        {
            EditorAssetUtility.EnsureFolder("Assets/Data/Npc");

            Sprite talk = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Icon_Talk.png");
            Sprite quest = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Icon_Quest.png");
            Sprite save = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Icon_Save.png");
            if (talk == null || quest == null || save == null)
            {
                Debug.LogError("InteractionIconBuilder: an icon image is missing (or not imported as Sprite) in " + ArtFolder);
                return null;
            }

            InteractionIconSet set = AssetDatabase.LoadAssetAtPath<InteractionIconSet>(SetPath);
            bool isNew = set == null;
            if (isNew)
            {
                set = ScriptableObject.CreateInstance<InteractionIconSet>();
            }

            SerializedObject serialized = new SerializedObject(set);
            serialized.FindProperty("talk").objectReferenceValue = talk;
            serialized.FindProperty("quest").objectReferenceValue = quest;
            serialized.FindProperty("save").objectReferenceValue = save;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                AssetDatabase.CreateAsset(set, SetPath);
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>
        /// Adds an icon above a character that is already in the open scene: a child object with a SpriteRenderer
        /// plus the InteractionIcon component on the character.
        /// </summary>
        /// <param name="target">The character or object in the scene.</param>
        /// <param name="type">Which icon to show.</param>
        /// <param name="height">Height of the icon CENTER above the target's pivot, in world units.</param>
        /// <param name="runner">The dialogue runner of the scene (the icon hides while it runs).</param>
        /// <param name="hideWhenFlag">Story flag that hides the icon when set (empty for none).</param>
        public static void AddIcon(GameObject target, InteractionIconType type, float height, DialogueRunner runner, string hideWhenFlag = "")
        {
            InteractionIconSet set = AssetDatabase.LoadAssetAtPath<InteractionIconSet>(SetPath);
            if (target == null || set == null)
            {
                Debug.LogError("InteractionIconBuilder: target or icon set missing. Run 'Build Interaction Icons' first.");
                return;
            }

            GameObject iconObject = new GameObject("InteractionIcon");
            iconObject.transform.SetParent(target.transform, false);
            iconObject.transform.localPosition = new Vector3(0f, height, 0f);

            SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
            iconRenderer.sortingOrder = IconSortingOrder;

            InteractionIcon icon = target.AddComponent<InteractionIcon>();
            EditorAssetUtility.SetReference(icon, "iconSet", set);
            EditorAssetUtility.SetReference(icon, "iconRenderer", iconRenderer);
            EditorAssetUtility.SetReference(icon, "runner", runner);
            EditorAssetUtility.SetString(icon, "hideWhenFlag", hideWhenFlag);

            SerializedObject serialized = new SerializedObject(icon);
            serialized.FindProperty("type").enumValueIndex = (int)type;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
