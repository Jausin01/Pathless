using System.Collections;
using Pathless.SceneFlow;
using UnityEngine;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Fades the whole screen to black and back. The fade itself is an Animator animation (states Clear, FadeOut,
    /// Black, FadeIn, driven by the bool "IsBlack"); the clips last 1 second, and the Animator speed is changed so
    /// the fade lasts the requested time.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        /// <summary>Animator of the full-screen black image.</summary>
        [SerializeField] private Animator animator;

        /// <summary>Animator parameter hash for the black state.</summary>
        private static readonly int IsBlackHash = Animator.StringToHash("IsBlack");

        /// <summary>
        /// When this scene was entered through a transition it must start fully black (the previous scene faded out),
        /// so the screen does not flash; SceneArrival fades it in afterwards.
        /// </summary>
        private void Awake()
        {
            if (SceneTransitionData.HasPending)
            {
                animator.SetBool(IsBlackHash, true);
                animator.Play("Black", 0, 0f);
            }
        }

        /// <summary>Fades to black or back to the scene and waits until the fade is done.</summary>
        /// <param name="toBlack">True to fade to black, false to fade back in.</param>
        /// <param name="duration">Fade time in seconds.</param>
        /// <returns>A coroutine that ends when the fade is finished.</returns>
        public IEnumerator Fade(bool toBlack, float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);

            // The clips are 1 second long, so speed = 1 / duration makes them last "duration" seconds.
            animator.speed = 1f / safeDuration;
            animator.SetBool(IsBlackHash, toBlack);

            yield return new WaitForSeconds(safeDuration);
        }
    }
}
