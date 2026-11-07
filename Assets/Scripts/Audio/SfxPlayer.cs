using UnityEngine;

namespace Pathless.Audio
{
    /// <summary>Plays one-shot sound effects by name (from the AudioLibrary).</summary>
    public class SfxPlayer : MonoBehaviour
    {
        /// <summary>Where the sound names come from.</summary>
        [SerializeField] private AudioLibrary library;

        /// <summary>The source that plays all effects.</summary>
        [SerializeField] private AudioSource source;

        /// <summary>Plays a sound effect.</summary>
        /// <param name="soundName">Name of the sound in the AudioLibrary.</param>
        /// <returns>The length of the sound in seconds, or 0 if there is no sound with that name.</returns>
        public float Play(string soundName)
        {
            AudioLibrary.Entry entry = library != null ? library.GetSound(soundName) : null;
            if (entry == null)
            {
                Debug.LogWarning($"SfxPlayer: no sound named '{soundName}' in the audio library.", this);
                return 0f;
            }

            source.PlayOneShot(entry.Clip, entry.Volume * GameVolume.SfxLevel);
            return entry.Clip.length;
        }
    }
}
