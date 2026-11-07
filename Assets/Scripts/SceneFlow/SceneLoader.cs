using System.Collections;
using Pathless.Cutscene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pathless.SceneFlow
{
    /// <summary>
    /// Changes the game to another scene: fades the screen to black, remembers where the player must appear and
    /// which conversation must continue, then loads the scene (one scene per area; the old scene is unloaded).
    /// Used by SceneTransitionZone (walking into a door) and by the "@scene" cutscene command.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>Fade used before loading, in seconds.</summary>
        private const float FadeSeconds = 0.6f;

        /// <summary>The screen fader used to go black before the load.</summary>
        [SerializeField] private ScreenFader fader;

        /// <summary>True while a transition is running, so a second request is ignored.</summary>
        private bool isLoading;

        /// <summary>Fades out and loads a scene. Does nothing if the scene is not in the Build Settings.</summary>
        /// <param name="sceneName">Name of the scene to load (the file name without ".unity").</param>
        /// <param name="spawnId">Id of the SceneSpawnPoint in the new scene (can be empty).</param>
        /// <param name="continueId">Id of the conversation to play after arriving (can be empty).</param>
        /// <returns>A coroutine; it normally never ends because the scene is replaced.</returns>
        public IEnumerator GoTo(string sceneName, string spawnId, string continueId)
        {
            if (isLoading)
            {
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"SceneLoader: scene '{sceneName}' is not in the Build Settings (run Tools > Pathless > Build Everything).", this);
                yield break;
            }

            isLoading = true;
            yield return StartCoroutine(fader.Fade(true, FadeSeconds));

            SceneTransitionData.Set(spawnId, continueId);
            SceneManager.LoadScene(sceneName);
        }
    }
}
