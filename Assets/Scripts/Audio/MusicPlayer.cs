using System.Collections;
using UnityEngine;

namespace Pathless.Audio
{
    /// <summary>
    /// Plays the background music with crossfades. It owns two AudioSources: while one fades out the other fades in.
    /// A track can be set for the scene ("sceneTrack", played when the scene starts). If the new scene asks for the
    /// same track that was playing in the previous scene, it continues from where it was instead of restarting.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        /// <summary>Where the track names come from.</summary>
        [SerializeField] private AudioLibrary library;

        /// <summary>First audio source.</summary>
        [SerializeField] private AudioSource sourceA;

        /// <summary>Second audio source.</summary>
        [SerializeField] private AudioSource sourceB;

        /// <summary>Track that starts when the scene starts (empty = keep silence).</summary>
        [SerializeField] private string sceneTrack = "";

        /// <summary>Fade time of the scene track, in seconds.</summary>
        [SerializeField] private float sceneFadeSeconds = 1f;

        /// <summary>Name of the track playing when the last scene closed (for continuing across scenes).</summary>
        private static string lastTrack = "";

        /// <summary>Playback position of that track, in seconds.</summary>
        private static float lastTime;

        /// <summary>The source that is audible now.</summary>
        private AudioSource active;

        /// <summary>The source that is silent (it receives the next track).</summary>
        private AudioSource idle;

        /// <summary>The running fade, if any.</summary>
        private Coroutine fade;

        /// <summary>Volume of the current track in the library, before master/music volumes (0 when silent).</summary>
        private float trackVolume;

        /// <summary>Name of the track playing now (empty when silent).</summary>
        public string CurrentTrack { get; private set; } = "";

        /// <summary>Clears the static memory when the game starts (needed because Enter Play Mode may skip domain reload).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lastTrack = "";
            lastTime = 0f;
        }

        /// <summary>Prepares the sources and starts the scene track.</summary>
        private void Start()
        {
            active = sourceA;
            idle = sourceB;
            foreach (AudioSource source in new[] { sourceA, sourceB })
            {
                source.playOnAwake = false;
                source.loop = true;
                source.volume = 0f;
            }

            if (!string.IsNullOrEmpty(sceneTrack))
            {
                Play(sceneTrack, sceneFadeSeconds);
            }
        }

        /// <summary>
        /// While no fade is running, keeps the audible source at the current master x music volume, so changes made
        /// in the Settings screen are heard immediately.
        /// </summary>
        private void Update()
        {
            if (fade == null && active != null)
            {
                active.volume = trackVolume * GameVolume.MusicLevel;
            }
        }

        /// <summary>Remembers the track and position so the next scene can continue it.</summary>
        private void OnDestroy()
        {
            lastTrack = CurrentTrack;
            lastTime = active != null && active.isPlaying ? active.time : 0f;
        }

        /// <summary>Changes the music with a crossfade. Does nothing if that track is already playing.</summary>
        /// <param name="trackName">Name of the track in the AudioLibrary.</param>
        /// <param name="fadeSeconds">Crossfade time in seconds (0 = immediate).</param>
        public void Play(string trackName, float fadeSeconds)
        {
            if (trackName == CurrentTrack)
            {
                return;
            }

            AudioLibrary.Entry entry = library != null ? library.GetMusic(trackName) : null;
            if (entry == null)
            {
                Debug.LogWarning($"MusicPlayer: no music named '{trackName}' in the audio library.", this);
                return;
            }

            // First track of this scene and it is the same one that was playing before: continue from its position.
            bool continues = CurrentTrack == "" && string.Equals(lastTrack, trackName, System.StringComparison.OrdinalIgnoreCase);
            float startTime = continues ? Mathf.Min(lastTime, entry.Clip.length - 0.05f) : 0f;

            CurrentTrack = trackName;
            StartFade(entry, continues ? 0f : fadeSeconds, startTime);
        }

        /// <summary>Fades the music out and stops it.</summary>
        /// <param name="fadeSeconds">Fade time in seconds (0 = immediate).</param>
        public void Stop(float fadeSeconds)
        {
            CurrentTrack = "";
            StartFade(null, fadeSeconds, 0f);
        }

        /// <summary>Starts a crossfade, replacing any fade already running.</summary>
        private void StartFade(AudioLibrary.Entry entry, float seconds, float startTime)
        {
            if (fade != null)
            {
                StopCoroutine(fade);
            }

            fade = StartCoroutine(Crossfade(entry, seconds, startTime));
        }

        /// <summary>Fades the active source out and, if there is a new entry, fades the idle source in.</summary>
        private IEnumerator Crossfade(AudioLibrary.Entry entry, float seconds, float startTime)
        {
            AudioSource fadingOut = active;
            AudioSource fadingIn = idle;
            float outStart = fadingOut.volume;
            trackVolume = entry != null ? entry.Volume : 0f;
            float inTarget = trackVolume * GameVolume.MusicLevel;

            if (entry != null)
            {
                fadingIn.clip = entry.Clip;
                fadingIn.time = startTime;
                fadingIn.volume = 0f;
                fadingIn.Play();
            }

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / seconds;
                fadingOut.volume = Mathf.Lerp(outStart, 0f, t);
                fadingIn.volume = Mathf.Lerp(0f, inTarget, t);
                yield return null;
            }

            fadingOut.Stop();
            fadingOut.volume = 0f;
            fadingIn.volume = inTarget;

            if (entry != null)
            {
                active = fadingIn;
                idle = fadingOut;
            }

            fade = null;
        }
    }
}
