using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Data asset that describes one speaking character: name shown in the dialogue box, voice blip sound
    /// and the portrait for each facial expression (128x128 sprites).
    /// Create one with Assets > Create > Pathless > Dialogue > Character, then reference it from dialogue lines.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Pathless/Dialogue/Character")]
    public class DialogueCharacter : ScriptableObject
    {
        /// <summary>
        /// One portrait entry: an expression name (for example "happy") and the sprite shown for it.
        /// </summary>
        [Serializable]
        public class ExpressionPortrait
        {
            /// <summary>Expression name written in dialogue lines (not case sensitive), for example "neutral".</summary>
            [SerializeField] private string expression;

            /// <summary>The 128x128 portrait sprite shown while this expression is used.</summary>
            [SerializeField] private Sprite sprite;

            /// <summary>Expression name of this entry.</summary>
            public string Expression => expression;

            /// <summary>Portrait sprite of this entry.</summary>
            public Sprite Sprite => sprite;
        }

        /// <summary>Name shown on the name plate of the dialogue box. Leave empty to hide the name plate.</summary>
        [SerializeField] private string displayName = "Unnamed";

        /// <summary>Short sound played while this character's text is typed (the "voice").</summary>
        [SerializeField] private AudioClip voiceBlip;

        /// <summary>Base pitch of the voice blip. Use different values to give each character a different voice.</summary>
        [SerializeField] private float voicePitch = 1f;

        /// <summary>
        /// Portraits by expression. The FIRST entry is the default, used when a line has no expression
        /// or asks for an expression this character does not have.
        /// </summary>
        [SerializeField] private List<ExpressionPortrait> portraits = new List<ExpressionPortrait>();

        /// <summary>Name shown on the name plate.</summary>
        public string DisplayName => displayName;

        /// <summary>Sound played for each typed letter (can be null for a silent character).</summary>
        public AudioClip VoiceBlip => voiceBlip;

        /// <summary>Base pitch of the voice blip.</summary>
        public float VoicePitch => voicePitch;

        /// <summary>
        /// Finds the portrait for an expression.
        /// </summary>
        /// <param name="expression">Expression name from the dialogue line; empty means "use the default".</param>
        /// <returns>The matching sprite, the first portrait as a fallback, or null if the character has no portraits.</returns>
        public Sprite GetPortrait(string expression)
        {
            if (portraits.Count == 0)
            {
                return null;
            }

            foreach (ExpressionPortrait entry in portraits)
            {
                if (string.Equals(entry.Expression, expression, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Sprite;
                }
            }

            return portraits[0].Sprite;
        }
    }
}
