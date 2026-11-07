using System.Collections.Generic;
using System.IO;
using Pathless.Audio;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the AudioLibrary asset from the audio files in Assets/Audio/Music (files named Music_Name) and
    /// Assets/Audio/Sfx (files named Sfx_Name). The name used in "@music name" / "@sfx name" is the file name without
    /// the "Music_" / "Sfx_" prefix and without a trailing "_Claude". Drop new files in those folders and run
    /// Tools > Pathless > Build Audio Library. The asset is updated in place, so references stay valid.
    /// </summary>
    public static class AudioBuilder
    {
        /// <summary>Where the library asset is saved.</summary>
        public const string LibraryPath = "Assets/Data/Audio/AudioLibrary.asset";

        /// <summary>Folder with the music files.</summary>
        private const string MusicFolder = "Assets/Audio/Music";

        /// <summary>Folder with the sound effect files.</summary>
        private const string SfxFolder = "Assets/Audio/Sfx";

        /// <summary>Menu entry that builds the library.</summary>
        [MenuItem("Tools/Pathless/Build Audio Library")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates or updates the library from the audio folders.</summary>
        /// <returns>True when the library was saved.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder("Assets/Data/Audio");
            EditorAssetUtility.EnsureFolder(MusicFolder);
            EditorAssetUtility.EnsureFolder(SfxFolder);

            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            bool isNew = library == null;
            if (isNew)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
            }

            SerializedObject serialized = new SerializedObject(library);
            int musicCount = FillList(serialized.FindProperty("music"), MusicFolder, "Music_");
            int soundCount = FillList(serialized.FindProperty("sounds"), SfxFolder, "Sfx_");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"AudioBuilder: {musicCount} music track(s) and {soundCount} sound(s) in the library.");
            return true;
        }

        /// <summary>
        /// Sets the track a scene starts with, on the MusicPlayer of the cutscene system object that is in the scene.
        /// </summary>
        /// <param name="cutsceneSystem">The CutsceneSystem instance in the open scene.</param>
        /// <param name="trackName">Track name in the library (empty for no music).</param>
        public static void SetSceneTrack(GameObject cutsceneSystem, string trackName)
        {
            MusicPlayer player = cutsceneSystem != null ? cutsceneSystem.GetComponentInChildren<MusicPlayer>() : null;
            if (player == null)
            {
                Debug.LogError("AudioBuilder: no MusicPlayer found on the cutscene system.");
                return;
            }

            EditorAssetUtility.SetString(player, "sceneTrack", trackName);
        }

        /// <summary>Fills one list property with an entry per audio file of a folder.</summary>
        /// <returns>How many entries were written.</returns>
        private static int FillList(SerializedProperty list, string folder, string prefix)
        {
            List<(string name, AudioClip clip)> found = new List<(string, AudioClip)>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    found.Add((CleanName(Path.GetFileNameWithoutExtension(path), prefix), clip));
                }
            }

            list.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("name").stringValue = found[i].name;
                entry.FindPropertyRelative("clip").objectReferenceValue = found[i].clip;
                entry.FindPropertyRelative("volume").floatValue = 1f;
            }

            return found.Count;
        }

        /// <summary>Turns "Music_Town_Claude" into "Town" (prefix and the "_Claude" suffix removed).</summary>
        private static string CleanName(string fileName, string prefix)
        {
            string name = fileName;
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
            }

            if (name.EndsWith("_Claude", System.StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - "_Claude".Length);
            }

            return name;
        }
    }
}
