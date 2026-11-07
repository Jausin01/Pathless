using UnityEngine;

namespace Pathless.Audio
{
    /// <summary>
    /// The volume levels chosen by the player (master, music, sound effects), each from 0 to 1.
    /// The audio players multiply their clips by these when they start a sound. The future Settings screen will
    /// change them here (and save them).
    /// </summary>
    public static class GameVolume
    {
        /// <summary>Overall volume.</summary>
        private static float master = 1f;

        /// <summary>Music volume.</summary>
        private static float music = 1f;

        /// <summary>Sound effects volume.</summary>
        private static float sfx = 1f;

        /// <summary>Overall volume from 0 to 1.</summary>
        public static float Master
        {
            get => master;
            set => master = Mathf.Clamp01(value);
        }

        /// <summary>Music volume from 0 to 1.</summary>
        public static float Music
        {
            get => music;
            set => music = Mathf.Clamp01(value);
        }

        /// <summary>Sound effects volume from 0 to 1.</summary>
        public static float Sfx
        {
            get => sfx;
            set => sfx = Mathf.Clamp01(value);
        }

        /// <summary>Master times music: what a music clip is multiplied by.</summary>
        public static float MusicLevel => master * music;

        /// <summary>Master times sound effects: what a sound effect is multiplied by.</summary>
        public static float SfxLevel => master * sfx;
    }
}
