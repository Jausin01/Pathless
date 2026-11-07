using Pathless.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the placeholder characters for the test scene (Elder and Messenger, four expressions each),
    /// fills the character library, and provides access to the test conversations, which live in the text file
    /// Assets/Dialogue/Test_Movement.dialogue (edit that file to change the test dialogue).
    /// Safe to run again: the character assets are replaced; the .dialogue file is never touched.
    /// </summary>
    public static class DialogueTestDataBuilder
    {
        /// <summary>Folder where the test characters are saved.</summary>
        private const string Folder = "Assets/Data/Dialogue/Test";

        /// <summary>Folder with the generated test portraits.</summary>
        private const string PortraitFolder = "Assets/Art/Portraits/Test_Claude";

        /// <summary>Path of the voice blip sound.</summary>
        private const string BlipPath = "Assets/Audio/Dialogue/TextBlip_Claude.wav";

        /// <summary>Path of the test dialogue text file.</summary>
        public const string ScriptPath = "Assets/Dialogue/Test_Movement.dialogue";

        /// <summary>Id of the conversation used by the optional block (tagged "optional" in the file).</summary>
        public const string OptionalId = "optional_chat";

        /// <summary>Id of the conversation used by the forced block (tagged "forced" in the file). It is the cutscene demo.</summary>
        public const string ForcedId = "cutscene_demo";

        /// <summary>Id of Violet's conversation (a portrait test between Violet and the Protagonist).</summary>
        public const string VioletId = "violet_chat";

        /// <summary>Id of Kate's conversation (a portrait test between Kate and the Protagonist).</summary>
        public const string KateId = "kate_chat";

        /// <summary>Id of Alice's conversation (a portrait test between Alice and the Protagonist).</summary>
        public const string AliceId = "alice_chat";

        /// <summary>Id of Gabe's conversation (a portrait test between Gabe and the Protagonist).</summary>
        public const string GabeId = "gabe_chat";

        /// <summary>Id of the sign conversation (narration lines only).</summary>
        public const string SignId = "sign_welcome";

        /// <summary>The expressions every test character has.</summary>
        private static readonly string[] Expressions = { "neutral", "happy", "angry", "sad" };

        /// <summary>Menu entry to rebuild only the test data.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Build Dialogue Test Data")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the characters, refreshes the library and imports the test dialogue file.</summary>
        /// <returns>True when everything was created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);

            // Old test conversations made by an earlier version (now they live in the .dialogue file).
            AssetDatabase.DeleteAsset(Folder + "/Sequence_Optional_Claude.asset");
            AssetDatabase.DeleteAsset(Folder + "/Sequence_Forced_Claude.asset");

            AudioClip blip = AssetDatabase.LoadAssetAtPath<AudioClip>(BlipPath);
            if (blip == null)
            {
                Debug.LogWarning("DialogueTestDataBuilder: " + BlipPath + " not found, characters will be silent.");
            }

            DialogueCharacter elder = CreateCharacter("Npc", "Elder", blip, 0.85f);
            DialogueCharacter messenger = CreateCharacter("Messenger", "Messenger", blip, 1.25f);
            if (elder == null || messenger == null)
            {
                return false;
            }

            AssetDatabase.SaveAssets();
            DialogueCharacterLibraryBuilder.Refresh();

            // Import after the library exists so the importer can check the character names.
            AssetDatabase.ImportAsset(ScriptPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        /// <summary>Loads one conversation (a sub-asset of the .dialogue file) by its id.</summary>
        /// <param name="id">The id written in the [id | tag] header.</param>
        /// <returns>The sequence, or null if the file or id does not exist.</returns>
        public static DialogueSequence LoadSequence(string id)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ScriptPath))
            {
                if (asset is DialogueSequence sequence && sequence.name == id)
                {
                    return sequence;
                }
            }

            Debug.LogError($"DialogueTestDataBuilder: conversation '{id}' not found in {ScriptPath}.");
            return null;
        }

        /// <summary>Creates one character asset with the four test portraits.</summary>
        /// <param name="fileKey">Part of the portrait file names, for example "Npc".</param>
        /// <param name="displayName">Name shown on the name plate (also the name used in .dialogue files).</param>
        /// <param name="blip">Voice sound.</param>
        /// <param name="pitch">Voice base pitch.</param>
        /// <returns>The created character, or null if a portrait is missing.</returns>
        private static DialogueCharacter CreateCharacter(string fileKey, string displayName, AudioClip blip, float pitch)
        {
            string path = $"{Folder}/Character_{fileKey}_Claude.asset";
            AssetDatabase.DeleteAsset(path);

            // All fields are set while the object is still in memory and it is saved at the end. Setting fields
            // on an asset that was just recreated at the same path lost the values (the name stayed "Unnamed").
            DialogueCharacter character = ScriptableObject.CreateInstance<DialogueCharacter>();

            SerializedObject serialized = new SerializedObject(character);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("voiceBlip").objectReferenceValue = blip;
            serialized.FindProperty("voicePitch").floatValue = pitch;

            SerializedProperty portraits = serialized.FindProperty("portraits");
            portraits.arraySize = Expressions.Length;
            for (int i = 0; i < Expressions.Length; i++)
            {
                string spritePath = $"{PortraitFolder}/Portrait_{fileKey}_{Expressions[i]}_Claude.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogError("DialogueTestDataBuilder: portrait not found (or not imported as Sprite): " + spritePath);
                    return null;
                }

                SerializedProperty entry = portraits.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("expression").stringValue = Expressions[i];
                entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(character, path);
            EditorUtility.SetDirty(character);
            return character;
        }
    }
}
