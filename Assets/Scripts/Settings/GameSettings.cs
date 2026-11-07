using Pathless.Audio;
using UnityEngine;

namespace Pathless.Settings
{
    /// <summary>
    /// The options the player can change in the Settings screen: the three volumes (stored in GameVolume) and the
    /// dialogue text speed. They are saved with PlayerPrefs (on this computer, separate from the story save) and
    /// loaded automatically when the game starts.
    /// </summary>
    public static class GameSettings
    {
        /// <summary>The text speed choices, as names shown to the player.</summary>
        public static readonly string[] TextSpeedNames = { "Slow", "Normal", "Fast" };

        /// <summary>How much faster each text speed choice is compared to the typewriter's own speed.</summary>
        private static readonly float[] TextSpeedMultipliers = { 0.6f, 1f, 1.8f };

        /// <summary>Default text speed choice (Normal).</summary>
        private const int DefaultTextSpeed = 1;

        /// <summary>PlayerPrefs key of the master volume.</summary>
        private const string MasterKey = "settings.volume.master";

        /// <summary>PlayerPrefs key of the music volume.</summary>
        private const string MusicKey = "settings.volume.music";

        /// <summary>PlayerPrefs key of the effects volume.</summary>
        private const string SfxKey = "settings.volume.sfx";

        /// <summary>PlayerPrefs key of the text speed.</summary>
        private const string TextSpeedKey = "settings.textSpeed";

        /// <summary>Chosen text speed index (0 = Slow, 1 = Normal, 2 = Fast).</summary>
        private static int textSpeed = DefaultTextSpeed;

        /// <summary>Chosen text speed index (0 = Slow, 1 = Normal, 2 = Fast).</summary>
        public static int TextSpeed
        {
            get => textSpeed;
            set => textSpeed = Mathf.Clamp(value, 0, TextSpeedMultipliers.Length - 1);
        }

        /// <summary>Factor applied to the typewriter speed (1 = its normal speed).</summary>
        public static float TextSpeedMultiplier => TextSpeedMultipliers[textSpeed];

        /// <summary>Reads the saved options when the game starts (before the first scene).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            GameVolume.Master = PlayerPrefs.GetFloat(MasterKey, 1f);
            GameVolume.Music = PlayerPrefs.GetFloat(MusicKey, 1f);
            GameVolume.Sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
            TextSpeed = PlayerPrefs.GetInt(TextSpeedKey, DefaultTextSpeed);
        }

        /// <summary>Writes the current options to PlayerPrefs.</summary>
        public static void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, GameVolume.Master);
            PlayerPrefs.SetFloat(MusicKey, GameVolume.Music);
            PlayerPrefs.SetFloat(SfxKey, GameVolume.Sfx);
            PlayerPrefs.SetInt(TextSpeedKey, textSpeed);
            PlayerPrefs.Save();
        }
    }
}
