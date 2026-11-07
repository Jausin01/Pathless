using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.Dialogue
{
    /// <summary>
    /// The visual side of the dialogue box: shows the speaker name, portrait and typed text, and plays the voice blip.
    /// It knows nothing about input or dialogue order (that is DialogueRunner's job).
    /// Slide in/out of the box and the bouncing "continue" arrow are Animator animations; this class only sets
    /// the Animator parameters.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        /// <summary>Text that shows the speaker name on the name plate.</summary>
        [SerializeField] private TMP_Text nameLabel;

        /// <summary>The name plate object; hidden when a line has no speaker name.</summary>
        [SerializeField] private GameObject namePlate;

        /// <summary>Typewriter that reveals the dialogue text.</summary>
        [SerializeField] private TypewriterEffect typewriter;

        /// <summary>The whole portrait area (frame + picture); hidden when the speaker has no portrait.</summary>
        [SerializeField] private GameObject portraitRoot;

        /// <summary>Image that displays the 128x128 portrait sprite.</summary>
        [SerializeField] private Image portraitImage;

        /// <summary>Animator that slides the dialogue box in and out (bool parameter "IsOpen").</summary>
        [SerializeField] private Animator boxAnimator;

        /// <summary>Animator of the continue arrow (bool parameter "IsWaiting").</summary>
        [SerializeField] private Animator indicatorAnimator;

        /// <summary>The text box rectangle; it is moved to the center when nobody is speaking.</summary>
        [SerializeField] private RectTransform textBoxRect;

        /// <summary>Horizontal position (canvas pixels) of the text box when someone speaks (next to the portrait).</summary>
        [SerializeField] private float speakerBoxX = 148f;

        /// <summary>Horizontal position of the text box when nobody speaks (centered in the 624 pixel panel).</summary>
        [SerializeField] private float narrationBoxX = 74f;

        /// <summary>Silhouette portrait shown when a speaker has no art for the requested expression.</summary>
        [SerializeField] private Sprite defaultPortrait;

        /// <summary>Voice blip used for speakers that have no character asset.</summary>
        [SerializeField] private AudioClip defaultVoiceBlip;

        /// <summary>Audio source that plays the voice blip.</summary>
        [SerializeField] private AudioSource voiceSource;

        /// <summary>A blip plays every this many revealed letters (2 sounds smoother than every letter).</summary>
        [SerializeField] private int lettersPerBlip = 2;

        /// <summary>Random pitch change (+/-) applied to each blip so the voice does not sound robotic.</summary>
        [SerializeField] private float pitchVariation = 0.08f;

        /// <summary>Animator parameter hash for the box open state.</summary>
        private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

        /// <summary>Animator parameter hash for the arrow state.</summary>
        private static readonly int IsWaitingHash = Animator.StringToHash("IsWaiting");

        /// <summary>Voice blip of the line being typed (null = silent).</summary>
        private AudioClip currentBlip;

        /// <summary>Base pitch of the line being typed.</summary>
        private float currentPitch = 1f;

        /// <summary>Counts revealed letters to know when the next blip is due.</summary>
        private int letterCounter;

        /// <summary>True while the typewriter is revealing text.</summary>
        public bool IsTyping => typewriter.IsTyping;

        /// <summary>Raised when the current line is fully visible.</summary>
        public event Action TypingFinished;

        /// <summary>Subscribes to the typewriter events.</summary>
        private void Awake()
        {
            typewriter.CharacterRevealed += OnCharacterRevealed;
            typewriter.Completed += OnTypingCompleted;
        }

        /// <summary>Unsubscribes from the typewriter events.</summary>
        private void OnDestroy()
        {
            if (typewriter != null)
            {
                typewriter.CharacterRevealed -= OnCharacterRevealed;
                typewriter.Completed -= OnTypingCompleted;
            }
        }

        /// <summary>Slides the box in.</summary>
        public void Open()
        {
            boxAnimator.SetBool(IsOpenHash, true);
        }

        /// <summary>Slides the box out and hides the arrow.</summary>
        public void Close()
        {
            SetWaiting(false);
            boxAnimator.SetBool(IsOpenHash, false);
        }

        /// <summary>Shows or hides the bouncing "continue" arrow.</summary>
        /// <param name="waiting">True when the line is complete and the game waits for the player.</param>
        public void SetWaiting(bool waiting)
        {
            indicatorAnimator.SetBool(IsWaitingHash, waiting);
        }

        /// <summary>
        /// Displays a line: name, portrait, box position and starts typing the text.
        /// With a speaker name the name plate and a portrait are shown (the default silhouette when the character
        /// has no art). Without any speaker (signs, narration) there is no name plate or portrait and the text box
        /// is centered.
        /// </summary>
        /// <param name="line">The line to show.</param>
        /// <param name="speaker">The resolved speaker asset, or null when unknown or when there is no speaker.</param>
        public void ShowLine(DialogueLine line, DialogueCharacter speaker)
        {
            letterCounter = 0;
            SetWaiting(false);

            // The character asset's name wins; otherwise the name written in the file is still shown.
            string speakerName = speaker != null && !string.IsNullOrEmpty(speaker.DisplayName) ? speaker.DisplayName : line.SpeakerName;
            bool someoneSpeaks = !string.IsNullOrEmpty(speakerName);

            namePlate.SetActive(someoneSpeaks);
            if (someoneSpeaks)
            {
                nameLabel.text = speakerName;
                // The plate resizes to fit the name, so the layout must be rebuilt right away.
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)namePlate.transform);
            }

            Sprite portrait = null;
            if (someoneSpeaks)
            {
                portrait = speaker != null ? speaker.GetPortrait(line.Expression) : null;
                if (portrait == null)
                {
                    portrait = defaultPortrait;
                }
            }

            portraitRoot.SetActive(portrait != null);
            portraitImage.sprite = portrait;

            // Only the horizontal position changes; the vertical position belongs to the slide Animator.
            textBoxRect.anchoredPosition = new Vector2(someoneSpeaks ? speakerBoxX : narrationBoxX, textBoxRect.anchoredPosition.y);

            // Voice: the character's own blip, the default blip for unknown speakers, silence for narration.
            currentBlip = speaker != null ? speaker.VoiceBlip : (someoneSpeaks ? defaultVoiceBlip : null);
            currentPitch = speaker != null ? speaker.VoicePitch : 1f;

            typewriter.Play(line.Text);
        }

        /// <summary>Completes the current line instantly.</summary>
        public void SkipTyping()
        {
            typewriter.Skip();
        }

        /// <summary>Plays the voice blip every few letters.</summary>
        /// <param name="letter">The letter that was just revealed.</param>
        private void OnCharacterRevealed(char letter)
        {
            if (currentBlip == null || char.IsWhiteSpace(letter))
            {
                return;
            }

            letterCounter++;
            if (letterCounter % Mathf.Max(1, lettersPerBlip) != 0)
            {
                return;
            }

            voiceSource.pitch = currentPitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            voiceSource.PlayOneShot(currentBlip);
        }

        /// <summary>Passes the typewriter completion on to the runner.</summary>
        private void OnTypingCompleted()
        {
            TypingFinished?.Invoke();
        }
    }
}
