using System.Globalization;
using UnityEngine;

namespace Pathless.Story
{
    /// <summary>
    /// Reads and checks the conditions written after "@if" in a .dialogue file.
    /// Forms: "flag" (is set), "!flag" (is not set), and "flag op number" with op one of == != &gt; &gt;= &lt; &lt;=
    /// (words separated by spaces), for example "elder_trust &gt;= 3".
    /// </summary>
    public static class StoryCondition
    {
        /// <summary>The comparison of a condition.</summary>
        private enum Comparison
        {
            /// <summary>No comparison: the flag itself is checked.</summary>
            None,
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }

        /// <summary>Checks the syntax of a condition without evaluating it.</summary>
        /// <param name="text">The condition text.</param>
        /// <param name="error">What is wrong, when the result is false.</param>
        /// <returns>True if the condition is valid.</returns>
        public static bool Validate(string text, out string error)
        {
            return TryParse(text, out _, out _, out _, out _, out error);
        }

        /// <summary>Evaluates a condition against the current flags. An invalid condition logs an error and is false.</summary>
        /// <param name="text">The condition text.</param>
        /// <returns>True if the condition holds.</returns>
        public static bool Evaluate(string text)
        {
            if (!TryParse(text, out string flag, out bool negate, out Comparison comparison, out float number, out string error))
            {
                Debug.LogError($"StoryCondition: invalid condition '{text}': {error}");
                return false;
            }

            float value = StoryFlags.Get(flag);
            switch (comparison)
            {
                case Comparison.Equal: return Mathf.Approximately(value, number);
                case Comparison.NotEqual: return !Mathf.Approximately(value, number);
                case Comparison.Greater: return value > number;
                case Comparison.GreaterOrEqual: return value >= number;
                case Comparison.Less: return value < number;
                case Comparison.LessOrEqual: return value <= number;
                default: return (value != 0f) != negate;
            }
        }

        /// <summary>Splits a condition into its parts.</summary>
        private static bool TryParse(string text, out string flag, out bool negate, out Comparison comparison, out float number, out string error)
        {
            flag = "";
            negate = false;
            comparison = Comparison.None;
            number = 0f;
            error = "";

            string[] words = text.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 1)
            {
                negate = words[0].StartsWith("!");
                flag = negate ? words[0].Substring(1) : words[0];
            }
            else if (words.Length == 3)
            {
                flag = words[0];
                switch (words[1])
                {
                    case "==": comparison = Comparison.Equal; break;
                    case "!=": comparison = Comparison.NotEqual; break;
                    case ">": comparison = Comparison.Greater; break;
                    case ">=": comparison = Comparison.GreaterOrEqual; break;
                    case "<": comparison = Comparison.Less; break;
                    case "<=": comparison = Comparison.LessOrEqual; break;
                    default:
                        error = $"'{words[1]}' is not a comparison (use == != > >= < <=).";
                        return false;
                }

                if (!float.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    error = $"'{words[2]}' is not a number.";
                    return false;
                }
            }
            else
            {
                error = "use 'flag', '!flag' or 'flag >= number' (separate the words with spaces).";
                return false;
            }

            if (!StoryFlags.IsValidName(flag))
            {
                error = $"'{flag}' is not a valid flag name.";
                return false;
            }

            return true;
        }
    }
}
