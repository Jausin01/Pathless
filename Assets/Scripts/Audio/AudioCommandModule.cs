using System.Collections;
using Pathless.Dialogue;
using UnityEngine;

namespace Pathless.Audio
{
    /// <summary>
    /// The audio commands of dialogue files:
    ///   @music name [seconds]    crossfades to a music track (default 1 second)
    ///   @music stop [seconds]    fades the music out
    ///   @sfx name [wait]         plays a sound effect; with "wait" the dialogue waits until it ends
    /// Names come from the AudioLibrary. Music commands return immediately; the fade runs by itself.
    /// </summary>
    public class AudioCommandModule : CommandModule
    {
        /// <summary>Default music fade time in seconds.</summary>
        private const float DefaultFadeSeconds = 1f;

        /// <summary>Plays the music.</summary>
        [SerializeField] private MusicPlayer musicPlayer;

        /// <summary>Plays the sound effects.</summary>
        [SerializeField] private SfxPlayer sfxPlayer;

        /// <summary>Handles "music" and "sfx".</summary>
        public override bool Handles(string verb)
        {
            return verb == "music" || verb == "sfx";
        }

        /// <summary>Runs one command.</summary>
        public override IEnumerator Run(string[] words, string command)
        {
            if (words.Length < 2)
            {
                Warn(command, "use '@music name [seconds]', '@music stop [seconds]' or '@sfx name [wait]'.");
                yield break;
            }

            if (words[0].ToLowerInvariant() == "sfx")
            {
                float length = sfxPlayer.Play(words[1]);
                if (words.Length > 2 && words[2].ToLowerInvariant() == "wait" && length > 0f)
                {
                    yield return new WaitForSeconds(length);
                }

                yield break;
            }

            float seconds = DefaultFadeSeconds;
            if (words.Length > 2 && !Pathless.Cutscene.CutsceneParsing.TryFloat(words[2], out seconds))
            {
                Warn(command, "the fade time is not a number.");
                yield break;
            }

            if (words[1].ToLowerInvariant() == "stop")
            {
                musicPlayer.Stop(seconds);
            }
            else
            {
                musicPlayer.Play(words[1], seconds);
            }
        }
    }
}
