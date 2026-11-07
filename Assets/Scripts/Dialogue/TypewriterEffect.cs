using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Reveals a TextMeshPro text one character at a time (Undertale style), with longer pauses after
    /// punctuation, and can be skipped to show the whole text at once.
    /// It only handles the text reveal; sound and UI are handled by DialogueView.
    /// Note: this is the one effect done in code instead of the Animator, because the duration depends on the
    /// text length and punctuation, which an animation clip cannot express.
    /// Usage: put it on the same object as the TextMeshPro text and call Play / Skip.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        /// <summary>Typing speed in characters per second. Higher is faster.</summary>
        [SerializeField] private float charactersPerSecond = 40f;

        /// <summary>Extra pause (seconds) after commas, semicolons and colons.</summary>
        [SerializeField] private float shortPause = 0.12f;

        /// <summary>Extra pause (seconds) after the end of a sentence (. ! ? and the ellipsis character).</summary>
        [SerializeField] private float longPause = 0.3f;

        /// <summary>The text component that is revealed.</summary>
        private TMP_Text label;

        /// <summary>The running reveal coroutine, or null when nothing is being typed.</summary>
        private Coroutine revealRoutine;

        /// <summary>True while characters are still being revealed.</summary>
        public bool IsTyping { get; private set; }

        /// <summary>Raised each time a character becomes visible. The argument is that character.</summary>
        public event Action<char> CharacterRevealed;

        /// <summary>Raised once when the whole text is visible (typed to the end or skipped).</summary>
        public event Action Completed;

        /// <summary>Caches the text component.</summary>
        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        /// <summary>Stops any running reveal when the object is disabled.</summary>
        private void OnDisable()
        {
            StopRoutine();
            IsTyping = false;
        }

        /// <summary>Starts typing a new text from the beginning.</summary>
        /// <param name="content">The text to type; rich text tags are allowed and are never typed as characters.</param>
        public void Play(string content)
        {
            StopRoutine();

            label.text = content;
            label.maxVisibleCharacters = 0;
            IsTyping = true;

            revealRoutine = StartCoroutine(RevealRoutine());
        }

        /// <summary>Shows the whole text immediately. Does nothing if the text is not being typed.</summary>
        public void Skip()
        {
            if (!IsTyping)
            {
                return;
            }

            StopRoutine();

            // A very large number makes TextMeshPro show every character.
            label.maxVisibleCharacters = int.MaxValue;
            Finish();
        }

        /// <summary>
        /// Reveals the characters over time. A timer is used (instead of one wait per character) so the
        /// speed stays correct even when several characters must appear in the same frame.
        /// </summary>
        private IEnumerator RevealRoutine()
        {
            // The text info is only valid after TextMeshPro has built the text mesh.
            label.ForceMeshUpdate();
            TMP_TextInfo info = label.textInfo;
            int total = info.characterCount;

            float baseDelay = 1f / Mathf.Max(1f, charactersPerSecond);
            int visible = 0;
            float timer = 0f;
            float currentDelay = 0f; // The first character appears immediately.

            while (visible < total)
            {
                timer += Time.deltaTime;

                while (visible < total && timer >= currentDelay)
                {
                    timer -= currentDelay;

                    char revealed = info.characterInfo[visible].character;
                    visible++;
                    label.maxVisibleCharacters = visible;
                    CharacterRevealed?.Invoke(revealed);

                    char next = visible < total ? info.characterInfo[visible].character : '\0';
                    currentDelay = GetDelayAfter(revealed, next, baseDelay);
                }

                yield return null;
            }

            revealRoutine = null;
            Finish();
        }

        /// <summary>Calculates how long to wait before revealing the character after the given one.</summary>
        /// <param name="revealed">The character that was just revealed.</param>
        /// <param name="next">The character that comes next ('\0' at the end of the text).</param>
        /// <param name="baseDelay">The normal delay between two characters.</param>
        /// <returns>The delay in seconds.</returns>
        private float GetDelayAfter(char revealed, char next, float baseDelay)
        {
            // Spaces and line breaks appear together with the next letter (no wait, no sound).
            if (char.IsWhiteSpace(revealed))
            {
                return 0f;
            }

            switch (revealed)
            {
                case ',':
                case ';':
                case ':':
                    return baseDelay + shortPause;

                case '.':
                case '!':
                case '?':
                case '…': // The single "…" character.
                    // In "..." or "?!" only the last mark gets the long pause.
                    return baseDelay + (IsSentenceEnd(next) ? shortPause : longPause);

                default:
                    return baseDelay;
            }
        }

        /// <summary>True for characters that end a sentence.</summary>
        /// <param name="character">The character to test.</param>
        /// <returns>True for . ! ? and the ellipsis character.</returns>
        private static bool IsSentenceEnd(char character)
        {
            return character == '.' || character == '!' || character == '?' || character == '…';
        }

        /// <summary>Marks the typing as finished and tells the listeners.</summary>
        private void Finish()
        {
            IsTyping = false;
            Completed?.Invoke();
        }

        /// <summary>Stops the reveal coroutine if it is running.</summary>
        private void StopRoutine()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
        }
    }
}
