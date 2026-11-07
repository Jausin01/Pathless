using Pathless.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the DialogueCharacter assets of the story characters that already have real portraits
    /// (Protagonist, Violet, Kate, Alice, Gabe, Michael and Hina), so they can speak in .dialogue files ("Violet | sad: ..."). Portraits are the
    /// author's own art in Assets/Art/Portraits/(Name)/Portrait_(Name)_(expression).png.
    /// "scared" is also registered as "shocked" because the same face fits both. Existing assets are updated in place.
    /// Run it with Tools > Pathless > Dialogue > Build Story Characters (also part of Build Everything).
    /// </summary>
    public static class StoryCharacterBuilder
    {
        /// <summary>Folder where the character assets are saved.</summary>
        private const string Folder = "Assets/Data/Dialogue/Characters";

        /// <summary>Path of the voice blip sound.</summary>
        private const string BlipPath = "Assets/Audio/Dialogue/TextBlip_Claude.wav";

        /// <summary>Menu entry that builds only the story characters.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Build Story Characters")]
        public static void BuildFromMenu()
        {
            if (Build())
            {
                DialogueCharacterLibraryBuilder.Refresh();
            }
        }

        /// <summary>Creates or updates the Protagonist and Violet characters.</summary>
        /// <returns>True when both were created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);
            AudioClip blip = AssetDatabase.LoadAssetAtPath<AudioClip>(BlipPath);

            // The voice pitches are provisional.
            bool protagonist = BuildCharacter("Protagonist", blip, 1.0f, new[] { "neutral", "happy", "angry", "scared" });
            bool violet = BuildCharacter("Violet", blip, 1.15f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            bool kate = BuildCharacter("Kate", blip, 1.1f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            bool alice = BuildCharacter("Alice", blip, 1.2f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            bool gabe = BuildCharacter("Gabe", blip, 0.9f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            bool michael = BuildCharacter("Michael", blip, 1.05f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            bool hina = BuildCharacter("Hina", blip, 1.1f, new[] { "neutral", "happy", "angry", "sad", "scared" });
            AssetDatabase.SaveAssets();
            return protagonist && violet && kate && alice && gabe && michael && hina;
        }

        /// <summary>Creates or updates one character from the portrait files of its folder.</summary>
        /// <param name="name">Character name (also the art folder and the name used in .dialogue files).</param>
        /// <param name="blip">Voice sound.</param>
        /// <param name="pitch">Voice base pitch.</param>
        /// <param name="expressions">The expression files that exist for this character.</param>
        /// <returns>False if a portrait is missing.</returns>
        private static bool BuildCharacter(string name, AudioClip blip, float pitch, string[] expressions)
        {
            string path = $"{Folder}/Character_{name}_Claude.asset";
            DialogueCharacter character = AssetDatabase.LoadAssetAtPath<DialogueCharacter>(path);
            bool isNew = character == null;
            if (isNew)
            {
                character = ScriptableObject.CreateInstance<DialogueCharacter>();
            }

            // Collect (expression name, sprite) pairs; "scared" also gets a "shocked" entry.
            System.Collections.Generic.List<(string expression, Sprite sprite)> entries =
                new System.Collections.Generic.List<(string, Sprite)>();
            foreach (string expression in expressions)
            {
                string spritePath = $"Assets/Art/Portraits/{name}/Portrait_{name}_{expression}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogError("StoryCharacterBuilder: portrait not found (or not imported as Sprite): " + spritePath);
                    return false;
                }

                entries.Add((expression, sprite));
                if (expression == "scared")
                {
                    entries.Add(("shocked", sprite));
                }
            }

            SerializedObject serialized = new SerializedObject(character);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("voiceBlip").objectReferenceValue = blip;
            serialized.FindProperty("voicePitch").floatValue = pitch;

            SerializedProperty portraits = serialized.FindProperty("portraits");
            portraits.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty entry = portraits.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("expression").stringValue = entries[i].expression;
                entry.FindPropertyRelative("sprite").objectReferenceValue = entries[i].sprite;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (isNew)
            {
                AssetDatabase.CreateAsset(character, path);
            }

            EditorUtility.SetDirty(character);
            return true;
        }
    }
}
