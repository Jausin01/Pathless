using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Audio
{
    /// <summary>
    /// Lists every music track and sound effect of the game by NAME, so dialogue files can use them
    /// ("@music town", "@sfx door"). Fill it with Tools > Pathless > Build Audio Library: it reads the files of
    /// Assets/Audio/Music (named Music_Name.wav/.ogg/.mp3) and Assets/Audio/Sfx (named Sfx_Name...). A trailing
    /// "_Claude" is ignored. Names are not case sensitive.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Pathless/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        /// <summary>One named sound.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Name used in commands.</summary>
            [SerializeField] private string name;

            /// <summary>The audio file.</summary>
            [SerializeField] private AudioClip clip;

            /// <summary>Volume of this clip from 0 to 1 (balance between files).</summary>
            [SerializeField, Range(0f, 1f)] private float volume = 1f;

            /// <summary>Name used in commands.</summary>
            public string Name => name;

            /// <summary>The audio file.</summary>
            public AudioClip Clip => clip;

            /// <summary>Volume of this clip.</summary>
            public float Volume => volume;
        }

        /// <summary>Music tracks (played in a loop).</summary>
        [SerializeField] private List<Entry> music = new List<Entry>();

        /// <summary>Sound effects (played once).</summary>
        [SerializeField] private List<Entry> sounds = new List<Entry>();

        /// <summary>Finds a music track by name.</summary>
        /// <param name="name">The track name.</param>
        /// <returns>The entry, or null if there is none with that name.</returns>
        public Entry GetMusic(string name)
        {
            return Find(music, name);
        }

        /// <summary>Finds a sound effect by name.</summary>
        /// <param name="name">The sound name.</param>
        /// <returns>The entry, or null if there is none with that name.</returns>
        public Entry GetSound(string name)
        {
            return Find(sounds, name);
        }

        /// <summary>Searches a list by name, ignoring case.</summary>
        private static Entry Find(List<Entry> list, string name)
        {
            foreach (Entry entry in list)
            {
                if (entry != null && entry.Clip != null && string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
