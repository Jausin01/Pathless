using Pathless.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the DialogueCharacterLibrary asset and fills it with every DialogueCharacter in the project.
    /// Run it after adding a new character: Tools > Pathless > Dialogue > Refresh Character Library.
    /// </summary>
    public static class DialogueCharacterLibraryBuilder
    {
        /// <summary>Where the library asset lives.</summary>
        public const string LibraryPath = "Assets/Data/Dialogue/CharacterLibrary.asset";

        /// <summary>Menu entry that refreshes the library.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Refresh Character Library")]
        public static void RefreshFromMenu()
        {
            Refresh();
        }

        /// <summary>Creates the library if needed and lists all characters in it.</summary>
        /// <returns>The library asset.</returns>
        public static DialogueCharacterLibrary Refresh()
        {
            EditorAssetUtility.EnsureFolder("Assets/Data/Dialogue");

            DialogueCharacterLibrary library = AssetDatabase.LoadAssetAtPath<DialogueCharacterLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<DialogueCharacterLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:DialogueCharacter");
            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty list = serialized.FindProperty("characters");
            list.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueCharacter>(path);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"DialogueCharacterLibraryBuilder: {guids.Length} character(s) in the library.");
            return library;
        }
    }
}
